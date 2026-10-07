using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Platforms
{
    public abstract class MockPlatform : PlatformBase
    {
        private readonly MockPlatformSocial friends;
        private readonly MockPlatformStats stats;
        public string LastOpenedProfileId => friends.LastOpenedProfileId;
        public bool SimulateFriendFailure { get => friends.SimulateFailure; set => friends.SimulateFailure = value; }

        protected MockPlatform(int number)
        {
            friends = new MockPlatformSocial(Session, number);
            stats = new MockPlatformStats(Session);
            Social = friends;
            Cloud = new MockPlatformCloud(Session);
            Restriction = new MockPlatformRestriction(Session, number);
            Stats = stats;
        }

        protected override async UniTask InitializeCoreAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.CompletedTask;
        }

        protected override void ReleaseCore()
        {
            stats.Reset();
        }
    }
}


