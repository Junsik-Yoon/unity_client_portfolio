using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Platforms
{
    public abstract class PlatformBaseStats
    {
        public abstract UniTask<int> GetStatAsync(string key, CancellationToken token);
        public abstract UniTask SetStatAsync(string key, int value, CancellationToken token);
        public abstract UniTask AddStatAsync(string key, int value, CancellationToken token);
        public abstract UniTask UnlockAchievementAsync(string id, CancellationToken token);
        public abstract UniTask<bool> IsAchievementUnlockedAsync(string id, CancellationToken token);
    }
}
