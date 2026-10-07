using System;
using System.Threading;
using Cysharp.Threading.Tasks;
namespace Portfolio.Platforms
{
    public sealed class NoPlatformRestriction : PlatformRestriction
    {
        private readonly PlatformSession session;
        public NoPlatformRestriction(PlatformSession session) => this.session = session;
        public override UniTask<bool> CheckInteractAllowedAsync(CancellationToken cancellationToken = default) => UnsupportedAsync(cancellationToken);
        public override UniTask<bool> CheckHasPlusAsync(bool showPlusUpsellDialog, CancellationToken cancellationToken = default) => UnsupportedAsync(cancellationToken);
        public override UniTask<bool> CheckLocalUserCommunicatableAsync(bool showMessage = false, CancellationToken cancellationToken = default) => UnsupportedAsync(cancellationToken);
        private async UniTask<bool> UnsupportedAsync(CancellationToken token)
        {
            using (session.CreateRequestScope(token))
            {
                await UniTask.CompletedTask;
                throw new NotSupportedException("플랫폼 없음 모드에서는 플랫폼 계정의 이용 제한을 검사하지 않습니다.");
            }
        }
    }
}
