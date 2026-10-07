using System;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Portfolio.Platforms
{
    internal sealed class MockPlatformCloud : PlatformBaseCloud
    {
        private readonly PlatformSession session;
        private string savedData;
        public MockPlatformCloud(PlatformSession session) => this.session = session;
        public override async UniTask UploadSaveDataAsync(string data, CancellationToken token)
        {
            using (var scope = session.CreateRequestScope(token))
            {
                if (data == null) throw new ArgumentNullException(nameof(data), "저장 데이터가 필요합니다.");
                await UniTask.CompletedTask;
                scope.Token.ThrowIfCancellationRequested();
                savedData = data;
            }
        }
        public override async UniTask<string> DownloadSaveDataAsync(CancellationToken token)
        {
            using (session.CreateRequestScope(token))
            {
                if (savedData == null) throw new InvalidOperationException("저장된 클라우드 데이터가 없습니다.");
                return await UniTask.FromResult(savedData);
            }
        }
        public override async UniTask<bool> IsSaveDataExistAsync(CancellationToken token)
        {
            using (session.CreateRequestScope(token)) return await UniTask.FromResult(savedData != null);
        }
        public override async UniTask DeleteSaveDataAsync(CancellationToken token)
        {
            using (session.CreateRequestScope(token)) { await UniTask.CompletedTask; savedData = null; }
        }
        internal override void Release() => savedData = null;
    }
}
