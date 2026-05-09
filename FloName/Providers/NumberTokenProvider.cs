namespace FloName.Providers
{
    internal class NumberTokenProvider : ITokenProvider
    {
        public bool CanHandle(string token) =>
            token == "n" || token == "N";

        public string Generate(TokenContext ctx) =>
            ctx.Token == "n"
                ? ctx.Random.Next(0, 10).ToString()
                : ctx.Random.Next(1, 10).ToString();
    }
}