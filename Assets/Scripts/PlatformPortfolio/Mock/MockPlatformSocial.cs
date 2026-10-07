using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Platforms
{
    internal sealed class MockPlatformSocial : PlatformBaseSocial
    {
        private readonly int number;
        public bool SimulateFailure { get; set; }
        public string LastOpenedProfileId { get; private set; }
        public MockPlatformSocial(PlatformSession session, int number) : base(session) => this.number = number;

        protected override async UniTask<PlatformUser> LoadLocalUserCoreAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return await UniTask.FromResult(new PlatformUser($"mock{number}:local", $"mock 플랫폼 {number} 사용자", true));
        }
        protected override async UniTask<List<PlatformUser>> LoadFriendsCoreAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (SimulateFailure) throw new InvalidOperationException("친구 조회 실패");
            var users = new List<PlatformUser>();
            for (var i = 1; i <= number + 1; i++)
                users.Add(new PlatformUser($"mock{number}:{i}", $"플랫폼 {number} 친구 {i}", i % 2 == 1));
            return await UniTask.FromResult(users);
        }
        protected override async UniTask ShowProfileCoreAsync(string platformId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.CompletedTask;
            LastOpenedProfileId = platformId;
        }
        protected override void ReleaseCore()
        {
            LastOpenedProfileId = null;
            SimulateFailure = false;
        }
    }
}
