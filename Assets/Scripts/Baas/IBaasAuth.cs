using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Backend
{
    public interface IBaasAuth
    {
        UniTask<BaasSession> LoginWithPlatformAsync(string provider, string authCode, CancellationToken token);
    }
}
