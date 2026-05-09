
namespace FloName.Providers
{
    public record TokenContext(
        string Token,           // raw token name e.g. "W", "A", "DATE:yyyy-MM-dd"
        string[] Dictionary,    // word list for current lang
        INameContext Context,    // for uniqueness/SEQ state
        Random Random           // injected random source
    );
}
