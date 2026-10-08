using Portfolio.DemoSupport;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Platforms
{
    public abstract class MockPlatform : PlatformBase
    {
        private readonly MockPlatformSocial friends;
        private readonly MockPlatformStats stats;
        private readonly int number;
        public override string AuthProvider => "MockPlatform" + number;
        public override bool SupportsBackendAuth => true;
        public bool SimulateAuthFailure { get; set; }
        public string LastOpenedProfileId => friends.LastOpenedProfileId;
        public bool SimulateFriendFailure { get => friends.SimulateFailure; set => friends.SimulateFailure = value; }

        protected MockPlatform(int number)
        {
            this.number = number;
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

        protected override async UniTask<string> GetAuthCodeCoreAsync(CancellationToken token)
        {
            await UniTask.Delay(400, ignoreTimeScale: true, cancellationToken: token);
            if (SimulateAuthFailure)
                throw new System.InvalidOperationException("Mock 플랫폼 인증 코드 발급 실패");
            var user = await Social.GetLocalUserAsync(token);
            return MockAuthCode.Create(AuthProvider, user.Id);
        }
    }
}


