using Portfolio.Platforms;
using Portfolio.Backend;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Login
{
    public enum BaasLoginState { SignedOut, GettingAuthCode, SigningIn, SignedIn, Offline, Failed }

    public sealed class LoginFlow : IDisposable
    {
        private readonly PlatformBase platform;
        private readonly IBaasAuth backend;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private UniTaskCompletionSource<BaasSession> pending;
        private BaasSession session;
        private bool disposed;
        public BaasLoginState State { get; private set; }
        public BaasSession Session => !disposed && platform.IsInitialized ? session : null;

        public LoginFlow(PlatformBase platform, IBaasAuth backend)
        {
            this.platform = platform ?? throw new ArgumentNullException(nameof(platform));
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public async UniTask<BaasSession> LoginAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (disposed) throw new ObjectDisposedException(nameof(LoginFlow));
            if (!platform.IsInitialized) throw new InvalidOperationException("플랫폼 초기화가 먼저 필요합니다.");
            if (!platform.SupportsBackendAuth) { State = BaasLoginState.Offline; return null; }
            if (Session != null) return Session;
            var _completion = pending;
            if (_completion == null)
            {
                _completion = new UniTaskCompletionSource<BaasSession>();
                pending = _completion;
                LoginOwnedAsync(_completion).Forget();
            }
            var _result = await _completion.Task.AttachExternalCancellation(token);
            token.ThrowIfCancellationRequested();
            if (disposed || !platform.IsInitialized)
                throw new OperationCanceledException("인증 중 플랫폼이 종료되었습니다.");
            return _result;
        }

        private async UniTask LoginOwnedAsync(UniTaskCompletionSource<BaasSession> completion)
        {
            try
            {
                using (var scope = platform.CreateAuthScope(lifetime.Token))
                {
                    State = BaasLoginState.GettingAuthCode;
                    var code = await platform.GetAuthCodeAsync(scope.Token);
                    scope.Token.ThrowIfCancellationRequested();
                    State = BaasLoginState.SigningIn;
                    var result = await backend.LoginWithPlatformAsync(platform.AuthProvider, code, scope.Token);
                    scope.Token.ThrowIfCancellationRequested();
                    if (result == null) throw new InvalidOperationException("백엔드 로그인 결과가 없습니다.");
                    session = result;
                    State = BaasLoginState.SignedIn;
                    pending = null;
                    completion.TrySetResult(result);
                }
            }
            catch (OperationCanceledException e)
            {
                session = null;
                State = BaasLoginState.SignedOut;
                pending = null;
                completion.TrySetCanceled(e.CancellationToken);
            }
            catch (Exception e)
            {
                session = null;
                State = disposed ? BaasLoginState.SignedOut : BaasLoginState.Failed;
                pending = null;
                completion.TrySetException(e);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            session = null;
            State = BaasLoginState.SignedOut;
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
