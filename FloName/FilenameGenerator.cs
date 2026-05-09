using FloName.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace FloName
{
    public class FilenameGenerator
    {
        #region Properties and fields
        private Dictionary<string, string[]> WordDictionaries { get; set; } = [];
        private readonly Random _random;
        private readonly ILogger<FilenameGenerator> _logger;
        private readonly TokenProviderRegistry _registry;

        #endregion

        #region Constructor and setup
        /// <summary>
        /// Constructor loads word dictionaries from the specified path and sets up token providers.
        /// </summary>
        /// <param name="dictsPath">The path to the directory containing dictionary files.</param>
        /// <param name="logger">Optional logger for logging information and errors.</param>
        /// <param name="random">Optional random number generator. Defaults to Random.Shared.  Only required for testing and advanced cases</param>
        public FilenameGenerator(string dictsPath = "dicts", ILogger<FilenameGenerator>? logger = null, Random? random = null)
        {
            _random = random ?? Random.Shared;
            _logger = logger ?? NullLogger<FilenameGenerator>.Instance;
            _registry = BuildDefaultRegistry();
            LoadData(dictsPath);
        }

        private TokenProviderRegistry BuildDefaultRegistry()
        {
            var registry = new TokenProviderRegistry(_logger);
            registry.Register(new WordTokenProvider());
            registry.Register(new AlphaTokenProvider());
            registry.Register(new NumberTokenProvider());
            registry.Register(new AlphaNumericTokenProvider());
            registry.Register(new DateTokenProvider());
            registry.Register(new SeqTokenProvider());
            return registry;
        }

        private void LoadData(string path)
        {
            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Loading dictionaries from {Path}", path);

            if (!Directory.Exists(path))
            {
                _logger.LogError("Dictionaries directory not found at '{Path}'.", path);
                throw new InvalidOperationException($"Dictionaries directory not found at '{path}'.");
            }

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var underscoreIndex = name.LastIndexOf('_');
                if (underscoreIndex < 0 || underscoreIndex == name.Length - 1)
                    throw new InvalidOperationException($"Filename '{name}' does not contain a valid language suffix.");

                var lang = name[(underscoreIndex + 1)..];
                var content = File.ReadAllText(file);

                string[]? dict;
                try
                {
                    dict = System.Text.Json.JsonSerializer.Deserialize<string[]>(content);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse dictionary file '{file}'.", file);
                    throw new InvalidOperationException($"Failed to parse dictionary file '{file}'.", ex);
                }

                if (dict == null)
                    throw new InvalidOperationException($"Filename '{name}' is empty or contains invalid data.");

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Loaded dictionary {Lang} with {Count} words", lang, dict.Length);

                WordDictionaries[lang] = dict;
            }

            if (WordDictionaries.Count == 0)
            {
                _logger.LogWarning("No dictionaries were loaded from path '{Path}'.", path);
                throw new InvalidOperationException(
                    $"No dictionaries were loaded from path '{path}'. " +
                    $"Please ensure the directory exists and contains valid JSON files.");
            }

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Loaded {Count} dictionaries", WordDictionaries.Count);
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Entry point for the fluent builder API.
        /// </summary>
        /// <param name="lang">Language code e.g. "en"</param>
        public FilenameBuilder For(string lang)
        {
            ValidateLang(lang);
            ValidateLangExists(lang);
            return new FilenameBuilder(this, lang);
        }

        /// <summary>
        /// Registers a custom token provider. Last registered wins on conflict.
        /// Returns this for fluent chaining:
        /// <code>var generator = new FilenameGenerator("dicts")
        ///    .RegisterProvider(new EmojiTokenProvider())
        ///    .RegisterProvider(new GuidTokenProvider());
        ///    </code>
        /// </summary>
        public FilenameGenerator RegisterProvider(ITokenProvider provider)
        {
            _registry.Register(provider);
            return this; // fluent
        }

        /// <summary>
        /// Returns supported language codes.
        /// </summary>
        public IReadOnlyList<string> SupportedLanguages()
            => WordDictionaries.Keys.ToList();

        #region Help methods
        /// <summary>
        /// Returns structured help information about supported tokens and modifiers.
        /// </summary>
        public static HelpResponse GetHelp() => new(
            Description: "Tokens are written as {token} inside a format string. Literal characters outside braces are passed through as-is.",
            Tokens:
            [
                new("{a}",           "Single lowercase letter (a-z)",             "{a}               → e.g. k"),
                new("{A}",           "Single uppercase letter (A-Z)",             "{A}               → e.g. K"),
                new("{n}",           "Single digit (0-9)",                        "{n}               → e.g. 4"),
                new("{N}",           "Single non-zero digit (1-9)",               "{N}               → e.g. 7"),
                new("{an}",          "Single lowercase letter or digit",          "{an}              → e.g. k4"),
                new("{An}",          "Single uppercase letter or digit",          "{An}              → e.g. K4"),
                new("{AN}/{aN}",     "Single uppercase letter or non-zero digit", "{AN}              → e.g. K4"),
                new("{w}",           "Random word, lowercase",                    "{w}               → e.g. river"),
                new("{W}",           "Random word, original casing",              "{W}               → e.g. River"),
                new("{SEQ}",         "Sequential counter",                        "{SEQ}             → 1, 2, 3"),
                new("{DATE:format}", "Current date/time using .NET format string. See https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings",
                                                                                  "{DATE:yyyy-MM-dd} → 2026-05-08"),
            ],
            Modifiers:
            [
                new(":N",         "Repeat token N times",                                       "{A:4}       → e.g. KXRM"),
                new(":N:sep",     "Repeat token N times with separator",                        "{W:2:-}     → e.g. River-Table"),
                new("U",          "Unique suffix — value not repeated in this generation call", "{AU}        → unique uppercase letter"),
                new(":start:pad", "SEQ start and pad width",                                    "{SEQ:5:3}   → 005, 006, 007"),
            ],
            Examples:
            [
                "{W}-{W}-{N:4}                       → e.g. River-Table-3847",
                "{date:yyyyMMdd}-{A:4}               → e.g. 20260508-KXRM",
                "{w}-{nU}{nU}{nU}                    → e.g. forest-492",
                "report_{DATE:yyyy-MM-dd}_{A:2}{n:3} → e.g. report_2026-05-08-XK291",
                "{W:3:-}                             → e.g. River-Table-Stone",
                "{SEQ:1:3}-{W}                       → e.g. 001-River",
            ]
        );

        /// <summary>
        /// Returns plain text help information about supported tokens and modifiers.
        /// </summary>
        public static string Help()
        {
            var help = GetHelp();
            var sb = new StringBuilder();

            sb.AppendLine("FloName Format String Reference");
            sb.AppendLine("================================");
            sb.AppendLine();
            sb.AppendLine(help.Description);
            sb.AppendLine();
            sb.AppendLine("TOKENS");
            sb.AppendLine("------");
            foreach (var t in help.Tokens)
                sb.AppendLine($"  {t.Token,-20} {t.Description,-50} {t.Example}");
            sb.AppendLine();
            sb.AppendLine("MODIFIERS");
            sb.AppendLine("---------");
            foreach (var m in help.Modifiers)
                sb.AppendLine($"  {m.Token,-20} {m.Description,-50} {m.Example}");
            sb.AppendLine();
            sb.AppendLine("EXAMPLES");
            sb.AppendLine("--------");
            foreach (var e in help.Examples)
                sb.AppendLine($"  {e}");

            return sb.ToString();
        }
        #endregion

        #endregion

        #region Internal methods — used by FilenameBuilder
        internal string GenerateName(string lang, string? extension, int wordCount, char separator)
        {
            if (wordCount <= 0 || wordCount > 100)
                throw new ArgumentOutOfRangeException(nameof(wordCount),
                    "wordCount must be between 1 and 100.");

            var dict = GetDictionary(lang);
            var normalizedExtension = NormalizeExtension(extension);

            var parts = new string[wordCount];
            for (int i = 0; i < wordCount; i++)
                parts[i] = dict[_random.Next(dict.Length)];

            return string.Join(separator, parts) + normalizedExtension;
        }

        internal string GenerateComplexName(string lang, string format, string? extension)
        {
            ValidateFormat(format);
            var normalizedExtension = NormalizeExtension(extension);

            var context = new NameContext();
            var generator = new NameGenerator(GetDictionary(lang), context, _registry, _random);
            return generator.GenerateComplexName(format) + normalizedExtension;
        }

        internal IReadOnlyList<string> GenerateBatch(string lang, string format, int count, string? extension)
        {
            ValidateCount(count);
            ValidateFormat(format);

            var normalizedExtension = NormalizeExtension(extension);
            var names = new List<string>(count);
            var dict = GetDictionary(lang);
            var seqContext = new NameContext();

            for (var i = 0; i < count; i++)
            {
                var uniqueContext = new NameContext(seqContext);
                var generator = new NameGenerator(dict, uniqueContext, _registry, _random);
                names.Add(generator.GenerateComplexName(format) + normalizedExtension);
            }

            return names;
        }
        #endregion

        #region Private methods
        private string[] GetDictionary(string lang)
        {
            if (!WordDictionaries.TryGetValue(lang, out var dict))
                throw new InvalidOperationException($"Language '{lang}' is not supported.");
            return dict;
        }

        private static string NormalizeExtension(string? extension)
        {
            if (string.IsNullOrEmpty(extension))
                return string.Empty;
            return extension.StartsWith('.') ? extension : "." + extension;
        }

        private static void ValidateLang(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang))
                throw new ArgumentException("Language must not be empty.", nameof(lang));
        }

        private void ValidateLangExists(string lang)
        {
            if (!WordDictionaries.ContainsKey(lang))
                throw new InvalidOperationException($"Language '{lang}' is not supported.");
        }

        private static void ValidateFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                throw new ArgumentException("Format must not be empty.", nameof(format));
            if (format.Length > 500)
                throw new ArgumentException("Format must not exceed 500 characters.", nameof(format));
        }

        private static void ValidateCount(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
            if (count > 10_000)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must not exceed 10,000.");
        }
        #endregion
    }
}