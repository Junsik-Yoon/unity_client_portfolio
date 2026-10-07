using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Platforms
{
    // 플랫폼 통계를 저장하지 않는다. 성공한 것처럼 처리하지 않고 미지원을 알린다.
    internal sealed class NoPlatformStats : PlatformBaseStats
    {
        private readonly PlatformSession session;
        public NoPlatformStats(PlatformSession session) => this.session = session;

        public override UniTask<int> GetStatAsync(string key, CancellationToken token)
        {
            using (session.CreateRequestScope(token)) throw Unsupported();
        }
        public override UniTask SetStatAsync(string key, int value, CancellationToken token)
        {
            using (session.CreateRequestScope(token)) throw Unsupported();
        }
        public override UniTask UnlockAchievementAsync(string id, CancellationToken token)
        {
            using (session.CreateRequestScope(token)) throw Unsupported();
        }
        public override UniTask<bool> IsAchievementUnlockedAsync(string id, CancellationToken token)
        {
            using (session.CreateRequestScope(token)) throw Unsupported();
        }
        public override UniTask AddStatAsync(string key, int value, CancellationToken token)
        {
            using (session.CreateRequestScope(token)) throw Unsupported();
        }
        private static NotSupportedException Unsupported()
            => new NotSupportedException("플랫폼 없음 모드에서는 플랫폼 통계·업적을 지원하지 않습니다.");
    }
}


