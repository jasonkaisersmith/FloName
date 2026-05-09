
namespace FloName.Providers
{
    internal class AlphaNumericTokenProvider : ITokenProvider
    {
        private const string LowerChars = "abcdefghijklmnopqrstuvwxyz0123456789";
        private const string LowerNoZero = "abcdefghijklmnopqrstuvwxyz123456789";
        private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string UpperNoZero = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";

        public bool CanHandle(string token) =>
            token == "an" || token == "An" || token == "AN" || token == "aN";

        public string Generate(TokenContext ctx)
        {
            var chars = ctx.Token switch
            {
                "an" => LowerChars,
                "An" => UpperChars,
                "AN" => UpperNoZero,
                "aN" => LowerNoZero,
                _ => LowerChars
            };
            return chars[ctx.Random.Next(chars.Length)].ToString();
        }
    }
}