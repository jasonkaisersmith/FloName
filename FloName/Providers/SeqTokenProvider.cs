
namespace FloName.Providers
{
    internal class SeqTokenProvider : ITokenProvider
    {
        public bool CanHandle(string token) =>
            token.StartsWith("SEQ", StringComparison.OrdinalIgnoreCase);

        public string Generate(TokenContext ctx)
        {
            var parts = ctx.Token.Split(':', 3);

            int start = 1;
            int pad = 0;

            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                if (!int.TryParse(parts[1], out start))
                    throw new FormatException(
                        $"Invalid start value in SEQ token: {{{ctx.Token}}}.");

            if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
                if (!int.TryParse(parts[2], out pad) || pad < 1)
                    throw new FormatException(
                        $"Invalid pad width in SEQ token: {{{ctx.Token}}}.");

            var value = ctx.Context.GetSequence(ctx.Token.ToUpperInvariant(), start);

            return pad > 0
                ? value.ToString().PadLeft(pad, '0')
                : value.ToString();
        }
    }
}