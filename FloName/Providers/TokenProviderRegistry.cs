using Microsoft.Extensions.Logging;

namespace FloName.Providers
{
    internal class TokenProviderRegistry
    {
        private readonly List<ITokenProvider> _providers = [];
        private readonly ILogger _logger;

        public TokenProviderRegistry(ILogger logger)
        {
            _logger = logger;
        }

        public void Register(ITokenProvider provider)
        {
            var existing = _providers.FirstOrDefault(p =>
                p.GetType() == provider.GetType());

            if (existing != null)
            {
                _logger.LogWarning(
                    "Token provider {Type} is already registered and will be replaced.",
                    provider.GetType().Name);
                _providers.Remove(existing);
            }

            // Check for token overlap with existing providers
            // We can't enumerate tokens easily, so warn at resolution time instead
            _providers.Add(provider); // last registered wins — added to end, resolved last-first
        }

        public ITokenProvider? Resolve(string token)
        {
            // Iterate in reverse — last registered wins
            for (int i = _providers.Count - 1; i >= 0; i--)
            {
                if (_providers[i].CanHandle(token))
                    return _providers[i];
            }
            return null;
        }
    }
}