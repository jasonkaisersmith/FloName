using System.Globalization;

namespace FloName.Providers
{
    internal class DateTokenProvider : ITokenProvider
    {
        public bool CanHandle(string token) =>
            token.StartsWith("DATE:", StringComparison.OrdinalIgnoreCase);

        public string Generate(TokenContext ctx)
        {
            var colonIndex = ctx.Token.IndexOf(':');
            if (colonIndex < 0 || colonIndex == ctx.Token.Length - 1)
                throw new FormatException(
                    $"Invalid DATE token: {{{ctx.Token}}}. Use {{DATE:format}}, e.g. {{DATE:yyyy-MM-dd}}.");

            var format = ctx.Token[(colonIndex + 1)..];
            return DateTime.Now.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}