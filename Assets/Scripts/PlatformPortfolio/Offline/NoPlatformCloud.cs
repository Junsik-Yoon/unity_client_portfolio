using System;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Portfolio.Platforms
{
    internal sealed class NoPlatformCloud : PlatformBaseCloud
    {
        private readonly PlatformSession session;
        public NoPlatformCloud(PlatformSession session) => this.session = session;
        public override async UniTask UploadSaveDataAsync(string data, CancellationToken token)
        {
            using (session.CreateRequestScope(token)) { await UniTask.CompletedTask; throw Unsupported(); }
        }
        public override async UniTask<string> DownloadSaveDataAsync(CancellationToken token)
        {
            using (session.CreateRequestScope(token)) { await UniTask.CompletedTask; throw Unsupported(); }
        }
        public override async UniTask<bool> IsSaveDataExistAsync(CancellationToken token)
        {
            using (session.CreateRequestScope(token)) { await UniTask.CompletedTask; throw Unsupported(); }
        }
        public override async UniTask DeleteSaveDataAsync(CancellationToken token)
        {
            using (session.CreateRequestScope(token)) { await UniTask.CompletedTask; throw Unsupported(); }
        }
        private static NotSupportedException Unsupported()
            => new NotSupportedException("플랫폼 없음 모드에서는 클라우드 저장을 지원하지 않습니다.");
        internal override void Release() { }
    }
}
