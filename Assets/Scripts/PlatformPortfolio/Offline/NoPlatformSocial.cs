using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Platforms
{
    internal sealed class NoPlatformSocial : PlatformBaseSocial
    {
        public NoPlatformSocial(PlatformSession session) : base(session) { }
        protected override async UniTask<PlatformUser> LoadLocalUserCoreAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return await UniTask.FromResult(new PlatformUser("offline:local", "오프라인 사용자", false));
        }
        protected override async UniTask<List<PlatformUser>> LoadFriendsCoreAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return await UniTask.FromResult(new List<PlatformUser>());
        }
        protected override async UniTask ShowProfileCoreAsync(string platformId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.CompletedTask;
            throw new NotSupportedException("플랫폼 없음 모드에서는 외부 프로필을 열 수 없습니다.");
        }
    }
}
