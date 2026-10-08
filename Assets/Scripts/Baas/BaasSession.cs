namespace Portfolio.Backend
{
    public sealed class BaasSession
    {
        public string PlayerId { get; }
        public string Provider { get; }
        public string PlatformUserId { get; }

        public BaasSession(string playerId, string provider, string platformUserId)
        {
            PlayerId = playerId;
            Provider = provider;
            PlatformUserId = platformUserId;
        }
    }
}
