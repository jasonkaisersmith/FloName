using FloName.Providers;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("Test_FloName")]

namespace FloName
{
    internal class NameGenerator
    {
        private readonly string[] _wordDictionary;
        private readonly INameContext _context;
        private readonly TokenProviderRegistry _registry;
        private readonly Random _random;

        public NameGenerator(string[] wordDictionary, INameContext context,
            TokenProviderRegistry registry, Random random)
        {
            _wordDictionary = wordDictionary;
            _context = context;
            _registry = registry;
            _random = random;
        }

        public string GenerateComplexName(string format)
        {
            var result = new StringBuilder();

            for (int i = 0; i < format.Length; i++)
            {
                if (format[i] == '{')
                {
                    var token = ReadToken(format, ref i);
                    result.Append(Evaluate(token));
                }
                else
                {
                    result.Append(format[i]);
                }
            }

            return result.ToString();
        }

        private static string ReadToken(string format, ref int i)
        {
            // i is currently on '{'
            i++; // skip '{'
            var sb = new StringBuilder();

            while (i < format.Length && format[i] != '}')
            {
                sb.Append(format[i]);
                i++;
            }

            if (i >= format.Length)
                throw new FormatException("Unterminated token: missing closing '}'");

            // i is now on '}', outer loop will i++ past it
            return sb.ToString();
        }

        private string Evaluate(string token)
        {
            // DATE handled first — has colon which would confuse repeat parsing
            if (token.StartsWith("DATE:", StringComparison.OrdinalIgnoreCase))
                return EvaluateViaProvider(token);

            // SEQ handled before repeat parsing — has its own colon syntax
            if (token.StartsWith("SEQ", StringComparison.OrdinalIgnoreCase))
                return EvaluateViaProvider(token);

            // Parse repeat count and optional separator
            int repeat = 1;
            string separator = string.Empty;
            var colonIndex = token.IndexOf(':');
            if (colonIndex >= 0)
            {
                var modifiers = token[(colonIndex + 1)..];
                token = token[..colonIndex];

                var modifierParts = modifiers.Split(':', 2);
                if (!int.TryParse(modifierParts[0], out repeat) || repeat < 1)
                    throw new FormatException($"Invalid repeat count in token: {{{token}}}");

                if (modifierParts.Length > 1)
                    separator = modifierParts[1];
            }

            // Parse U uniqueness suffix
            bool unique = false;
            if (token.Length > 1 && token.EndsWith("U", StringComparison.OrdinalIgnoreCase))
            {
                unique = true;
                token = token[..^1];
            }

            string BuildValue()
            {
                if (repeat == 1)
                    return EvaluateViaProvider(token);

                var parts = new string[repeat];
                for (int i = 0; i < repeat; i++)
                    parts[i] = EvaluateViaProvider(token);

                return string.Join(separator, parts);
            }

            return unique
                ? EnsureUnique(token, BuildValue)
                : BuildValue();
        }

        private string EvaluateViaProvider(string token)
        {
            var provider = _registry.Resolve(token);
            if (provider == null)
                throw new FormatException($"Unknown token: {{{token}}}");

            var ctx = new TokenContext(token, _wordDictionary, _context, _random);
            return provider.Generate(ctx);
        }

        private string EnsureUnique(string key, Func<string> generator)
        {
            int maxAttempts = GetPoolSize(key);

            for (int i = 0; i < maxAttempts; i++)
            {
                var value = generator();
                if (_context.RegisterUnique(key, value))
                    return value;
            }

            throw new InvalidOperationException(
                $"Could not generate a unique value for token '{key}' after {maxAttempts} attempts. " +
                $"The pool of possible values may be exhausted.");
        }

        private static int GetPoolSize(string token) => token switch
        {
            "a" or "A" => 26,
            "n" => 10,
            "N" => 9,
            "an" or "An" or "AN" => 36,
            "W" or "w" => 50,
            _ => 50 // default pool size for custom providers, which may vary widely
        };
    }
}