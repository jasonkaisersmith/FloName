namespace FloName.Providers
{
    internal class AlphaTokenProvider : ITokenProvider
    {
        public bool CanHandle(string token) =>
            token == "a" || token == "A";

        public string Generate(TokenContext ctx) =>
            ctx.Token == "a"
                ? ((char)('a' + ctx.Random.Next(26))).ToString()
                : ((char)('A' + ctx.Random.Next(26))).ToString();
    }
}