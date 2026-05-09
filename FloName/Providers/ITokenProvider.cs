namespace FloName.Providers
{
    public interface ITokenProvider
    {
        /// <summary>
        /// Returns true if this provider can handle the given token name.
        /// Token name is the raw string inside braces before any modifiers e.g. "W", "A", "DATE"
        /// </summary>
        bool CanHandle(string token);

        /// <summary>
        /// Generates a single value for the token. Called once per repeat.
        /// </summary>
        string Generate(TokenContext ctx);
    }
}