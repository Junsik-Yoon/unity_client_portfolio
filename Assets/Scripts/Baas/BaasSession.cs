using System.Threading;

namespace Portfolio.Backend
{
    public sealed class BaasSession
    {
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        public bool IsActive { get; private set; } = true;
        internal CancellationToken LifetimeToken => lifetime.Token;
        internal void Invalidate()
        {
            if (!IsActive) return;
            IsActive = false;
            lifetime.Cancel();
            lifetime.Dispose();
        }
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
