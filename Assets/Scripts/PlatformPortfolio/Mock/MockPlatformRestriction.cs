using System.Threading;
using Cysharp.Threading.Tasks;
namespace Portfolio.Platforms
{
    // 시연용 정책. 실제 플랫폼 계정이나 구독 상태를 조회하지 않는다.
    public sealed class MockPlatformRestriction : PlatformRestriction
    {
        private readonly PlatformSession session;
        private readonly int number;
        public bool LastDialogRequested { get; private set; }
        public MockPlatformRestriction(PlatformSession session, int number) { this.session = session; this.number = number; }
        public override UniTask<bool> CheckInteractAllowedAsync(CancellationToken cancellationToken = default) => CheckAsync(number != 3, false, cancellationToken);
        public override UniTask<bool> CheckHasPlusAsync(bool showPlusUpsellDialog, CancellationToken cancellationToken = default) => CheckAsync(number != 2, showPlusUpsellDialog, cancellationToken);
        public override UniTask<bool> CheckLocalUserCommunicatableAsync(bool showMessage = false, CancellationToken cancellationToken = default) => CheckAsync(number != 3, showMessage, cancellationToken);
        private async UniTask<bool> CheckAsync(bool allowed, bool showDialog, CancellationToken token)
        {
            using (var scope = session.CreateRequestScope(token))
            {
                await UniTask.CompletedTask;
                scope.Token.ThrowIfCancellationRequested();
                LastDialogRequested = showDialog;
                return allowed;
            }
        }
    }
}
