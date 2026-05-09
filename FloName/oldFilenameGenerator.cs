using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace FloName
{
    public class oldFilenameGenerator
    {
        private static readonly string helpText = """
        FloName Format String Reference
        ================================

        Tokens are written as {token} inside a format string.
        Literal characters outside braces are passed through as-is.

        RANDOM CHARACTERS
        -----------------
        {a}        Single lowercase letter (a-z)
        {A}        Single uppercase letter (A-Z)
        {n}        Single digit (0-9)
        {N}        Single non-zero digit (1-9)
        {an}       Single lowercase letter or digit
        {An}       Single uppercase letter or digit
        {AN}/{aN}  Single uppercase letter or non-zero digit

        WORDS
        -----
        {w}        Random word, lowercase
        {W}        Random word, PascalCase

        SEQUENCE NUMBERS
        ---
        {SEQ}          → 1, 2, 3        (default start=1, no padding)
        {SEQ:5}        → 5, 6, 7        (start at 5, no padding)
        {SEQ::3}       → 001, 002, 003  (default start=1, pad to 3 digits)
        {SEQ:5:3}      → 005, 006, 007  (start at 5, pad to 3 digits)

        DATE
        ----
        {DATE:format}   Current date/time using any .NET format string.
                        DATE is case-insensitive.
                        See: https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings

        MODIFIERS
        ---------
        :N   Repeat — generate the token N times
             {A:4}    → e.g. KXRM
             {w:2}    → e.g. tableriver  (use literals to separate: {w}-{w})
             {W:2:-}  → e.g. River-Table  (repeat with separator: {token:count:separator})

        U    Unique suffix — ensures value hasn't been generated before in this call
             {AU}     → unique uppercase letter
             {A:3U}   → unique 3-character string
             U is case-insensitive. Uniqueness is scoped to a single GenerateComplexName call.

        EXAMPLES
        --------
        {W}-{W}-{N:4}                          → River-Table-3847
        {date:yyyyMMdd}-{A:4}                  → 20260508-KXRM
        {w}-{nU}{nU}{nU}                       → forest-492
        report_{DATE:yyyy-MM-dd}_{A:2}{n:3}    → report_2026-05-08-XK291
        """;
        #region properties and fields
        private Dictionary<string, string[]> WordDictionaries { get; set; } = [];
        private readonly ILogger<FilenameGenerator> _logger;


        #endregion

        #region Constructor and setup
        public FilenameGenerator(string dictsPath = "dicts", ILogger<FilenameGenerator>? logger = null)
        {
            _logger = logger ?? NullLogger<FilenameGenerator>.Instance;
            LoadData(dictsPath);
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
                {
                    throw new InvalidOperationException($"Filename '{name}' does not contain a valid language suffix.");
                }

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

                this.WordDictionaries[lang] = dict;                
            }

            if (WordDictionaries == null || WordDictionaries?.Count == 0)
            {
                _logger.LogWarning("No dictionaries were loaded from path '{Path}'. Please ensure the directory exists and contains valid JSON files.", path);
                throw new InvalidOperationException($"No dictionaries were loaded from path '{path}'. Please ensure the directory exists and contains valid JSON files.");
            }

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Loaded {Count} dictionaries", WordDictionaries!.Count);
        }
        #endregion

        #region Public methods
        public FilenameBuilder For(string lang) => new FilenameBuilder(this, lang);

        /// <summary>
        /// Generates a simple name based on words
        /// </summary>
        /// <param name="lang">Dictionary language to use</param>
        /// <param name="extension">optional:  extension of file</param>
        /// <param name="wordCount">optional: number of words to include in the name</param>
        /// <param name="separator">optional: character to separate words in the name</param>
        /// <returns>Generated name as a string</returns>
        public string GenerateName(string lang, string extension = ".txt", int wordCount = 2, char separator = '_')
        {
            if (wordCount<=0 || wordCount > 100) 
                throw new InvalidOperationException($"wordCount of '{wordCount}' is invalid. wordCount must be between 1 and 100.");

            ValidateLang(lang);

            var dict = GetDictionary(lang);
            var normalizedExtension = NormalizeExtension(extension);

            var parts = new string[wordCount];
            for (int i = 0; i < wordCount; i++)
                parts[i] = dict[Random.Shared.Next(dict.Length)];

            return string.Join(separator, parts) + normalizedExtension;
        }

        /// <summary>
        /// Generates a complex name string using the specified language, format, file extension, and separator.
        /// Call "Help()" for details on the format syntax and available tokens.
        /// </summary>
        /// <remarks>The generated name is constructed based on the provided language and format, and
        /// always includes the specified or default file extension. The separator character is used internally to join
        /// name components according to the format.</remarks>
        /// <param name="lang">The language code used to select the dictionary for name generation. Cannot be null or empty.</param>
        /// <param name="format">The format string that defines the structure of the generated name. Must be a valid format recognized by the
        /// name generator.</param>
        /// <param name="extension">The file extension to append to the generated name. Defaults to ".txt" if not specified.</param>
        /// <param name="separator">The character used to separate parts of the generated name. Defaults to '_'.</param>
        /// <returns>A string containing the generated complex name with the specified extension appended.</returns>
        public string GenerateComplexName(string lang, string format, string? extension = ".txt")
        {
            ValidateFormat(format);
            ValidateLang(lang);
            var normalizedExtension = NormalizeExtension(extension);

            var context = new NameContext(); // fresh per run/file
            var generator = new NameGenerator(GetDictionary(lang), context);
            var coreName = generator.GenerateComplexName(format);

            return coreName + normalizedExtension;
        }

        /// <summary>
        /// Generates a batch of complex names strings using the specified language, format, file extension, and separator.
        /// Call "Help()" for details on the format syntax and available tokens.
        /// </summary>
        /// <remarks>The generated name is constructed based on the provided language and format, and
        /// always includes the specified or default file extension. The separator character is used internally to join
        /// name components according to the format.</remarks>
        /// <param name="lang">The language code used to select the dictionary for name generation. Cannot be null or empty.</param>
        /// <param name="format">The format string that defines the structure of the generated name. Must be a valid format recognized by the
        /// name generator.</param>
        /// <param name="extension">The file extension to append to the generated name. Defaults to ".txt" if not specified.</param>
        /// <param name="separator">The character used to separate parts of the generated name. Defaults to '_'.</param>
        /// <param name="count">The number of names to generate in the batch. Must be greater than zero.</param>
        /// <returns>A readonly list of strings containing the generated complex names with the specified extension appended.</returns>
        /// <exception cref="ArgumentOutOfRangeException">count must be greater than zero</exception>
        public IReadOnlyList<string> GenerateBatch(string lang, string format, int count, string? extension = ".txt")
        {
            ValidateCount(count);
            ValidateFormat(format);
            ValidateLang(lang);

            var normalizedExtension = NormalizeExtension(extension);
            var names = new List<string>(count);
            var dict = GetDictionary(lang);          // lookup once
            var seqContext = new NameContext();       // shared across batch — SEQ increments

            for (var i = 0; i < count; i++)
            {
                var uniqueContext = new NameContext(seqContext);  // fresh uniqueness, shared SEQ
                var generator = new NameGenerator(dict, uniqueContext);
                names.Add(generator.GenerateComplexName(format) + normalizedExtension);
            }

            return names;
        }

        public static string Help()
        {
            var help = FilenameGenerator.GetHelp();
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

        public static HelpResponse GetHelp() => new HelpResponse(
            Description: "Tokens are written as {token} inside a format string. Literal characters outside braces are passed through as-is.",
            Tokens:
            [
                new("{a}",           "Single lowercase letter (a-z)",             "{a}              → e.g. k"),
                new("{A}",           "Single uppercase letter (A-Z)",             "{A}              → e.g. K"),
                new("{n}",           "Single digit (0-9)",                        "{n}              → e.g. 4"),
                new("{N}",           "Single non-zero digit (1-9)",               "{N}              → e.g. 7"),
                new("{an}",          "Single lowercase letter or digit",          "{an}             → e.g. k4"),
                new("{An}",          "Single uppercase letter or digit",          "{An}             → e.g. K4"),
                new("{AN}/{aN}",     "Single uppercase letter or non-zero digit", "{AN}             → e.g. K4"),
                new("{w}",           "Random word, lowercase",                    "{w}              → e.g. river"),
                new("{W}",           "Random word, original casing",              "{W}              → e.g. River"),
                new("{SEQ}",         "Sequential counter",                        "{SEQ}            → 1, 2, 3"),
                new("{DATE:format}", "Current date/time using .NET format string. See https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings",
                                                                                  "{DATE:yyyy-MM-dd}→ 2026-05-08"),
            ],
            Modifiers:
            [
                new(":N",       "Repeat token N times",                                        "{A:4}       → e.g. KXRM"),
                new(":N:sep",   "Repeat token N times with separator",                         "{W:2:-}     → e.g. River-Table"),
                new("U",        "Unique suffix — value not repeated in this generation call",  "{AU}        → unique uppercase letter"),
                new(":start:pad", "SEQ start and pad width",                                   "{SEQ:5:3}   → 005, 006, 007"),
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
        #endregion

        #region private methods
        private string[] GetDictionary(string lang)
        {
            if (!this.WordDictionaries.TryGetValue(lang, out var dict))
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

        public IReadOnlyList<string> SupportedLanguages()
        {
            return WordDictionaries?.Keys.ToList() ?? new List<string>();
        }
        #endregion
    }
}
