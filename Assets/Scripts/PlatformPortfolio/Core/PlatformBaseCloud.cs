using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Platforms
{
    public abstract class PlatformBaseCloud
    {
        public abstract UniTask UploadSaveDataAsync(string data, CancellationToken token);
        public abstract UniTask<string> DownloadSaveDataAsync(CancellationToken token);
        public abstract UniTask<bool> IsSaveDataExistAsync(CancellationToken token);
        public abstract UniTask DeleteSaveDataAsync(CancellationToken token);
        internal abstract void Release();
    }
}
