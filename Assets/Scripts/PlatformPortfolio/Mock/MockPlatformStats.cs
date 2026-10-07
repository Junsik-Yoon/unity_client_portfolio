using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Platforms
{
    // 메모리 안에서만 유지되는 가상 통계. 실제 서버 저장·동기화 기능은 없다.
    internal sealed class MockPlatformStats : PlatformBaseStats
    {
        private readonly PlatformSession session;

        private readonly Dictionary<string, int> values = new Dictionary<string, int>();
        private readonly HashSet<string> achievements = new HashSet<string>();

        public MockPlatformStats(PlatformSession session)
        {
            this.session = session;

        }

        public override UniTask<int> GetStatAsync(string key, CancellationToken token)
            => ExecuteAsync(() =>
            {
                ValidateKey(key);
                return values.TryGetValue(key, out var value) ? value : 0;
            }, token);

        public override async UniTask SetStatAsync(string key, int value, CancellationToken token)
        {
            await ExecuteAsync(() =>
            {
                ValidateKey(key);
                values[key] = value;
                return true;
            }, token);
        }

        public override async UniTask UnlockAchievementAsync(string id, CancellationToken token)
        {
            await ExecuteAsync(() =>
            {
                ValidateKey(id);
                achievements.Add(id);
                return true;
            }, token);
        }

        public override UniTask<bool> IsAchievementUnlockedAsync(string id, CancellationToken token)
            => ExecuteAsync(() =>
            {
                ValidateKey(id);
                return achievements.Contains(id);
            }, token);

        private async UniTask<T> ExecuteAsync<T>(Func<T> action, CancellationToken token)
        {
            using (var scope = session.CreateRequestScope(token))
            {
                await UniTask.CompletedTask;
                scope.Token.ThrowIfCancellationRequested();
                return action();
            }
        }

        public override async UniTask AddStatAsync(string key, int value, CancellationToken token)
        {
            await ExecuteAsync(() =>
            {
                ValidateKey(key);
                values.TryGetValue(key, out var current);
                values[key] = checked(current + value);
                return true;
            }, token);
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("키가 필요합니다.", nameof(key));
        }

        public void Reset()
        {
            values.Clear();
            achievements.Clear();
        }
    }
}


