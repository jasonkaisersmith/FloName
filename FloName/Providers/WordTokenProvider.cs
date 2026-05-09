namespace FloName.Providers
{
    internal class WordTokenProvider : ITokenProvider
    {
        public bool CanHandle(string token) =>
            token == "w" || token == "W";

        public string Generate(TokenContext ctx)
        {
            var word = ctx.Dictionary[ctx.Random.Next(ctx.Dictionary.Length)];
            return ctx.Token == "w" ? word.ToLower() : word;
        }
    }
}