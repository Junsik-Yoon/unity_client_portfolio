using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Platforms
{
    // 실무 경험을 바탕으로 재구성한 예제. 실제 sdk 미포함
    public abstract class PlatformBase
    {
        protected readonly PlatformSession Session = new PlatformSession();
        private bool initializationPending;
        private bool releaseInProgress;

        public PlatformState State => Session.State;
        public bool IsInitialized => State == PlatformState.Ready;
        public PlatformBaseSocial Social { get; protected set; }
        public PlatformBaseStats Stats { get; protected set; }
        public PlatformBaseCloud Cloud { get; protected set; }
        public PlatformRestriction Restriction { get; protected set; }
        public virtual string AuthProvider => "Offline";
        public virtual bool SupportsBackendAuth => false;

        // 플랫폼 세션 종료 시 인증 발급과 백엔드 요청도 함께 취소한다.
        internal CancellationTokenSource CreateAuthScope(CancellationToken token) => Session.CreateRequestScope(token);

        public async UniTask<string> GetAuthCodeAsync(CancellationToken token)
        {
            using (var _scope = CreateAuthScope(token))
            {
                if (!SupportsBackendAuth)
                    throw new NotSupportedException("오프라인 플랫폼은 인증 코드를 발급하지 않습니다");
                var _autoCode = await GetAuthCodeCoreAsync(_scope.Token);
                _scope.Token.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(_autoCode))
                    throw new InvalidOperationException("플랫폼 인증 코드가 비어 있습니다.");
                return _autoCode;
            }
        }

        protected virtual UniTask<string> GetAuthCodeCoreAsync(CancellationToken token)
            => throw new NotSupportedException("구현이 필요합니다.");

        public async UniTask InitializeAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (IsInitialized)
            {
                return;
            }
            if (State != PlatformState.Uninitialized)
            {
                throw new InvalidOperationException("초기화 또는 종료가 진행 중입니다.");
            }

            var _sessionToken = Session.BeginInitialization();
            initializationPending = true;

            try
            {
                using (var _linked = CancellationTokenSource.CreateLinkedTokenSource(token, _sessionToken))
                {
                    await InitializeCoreAsync(_linked.Token);
                    await Social.InitializeAsync(_linked.Token);
                    _linked.Token.ThrowIfCancellationRequested();
                    Session.MarkReady();
                }
            }
            catch
            {
                Release();
                throw;
            }
            finally
            {
                initializationPending = false;
                if (State == PlatformState.Releasing && !releaseInProgress)
                {
                    Session.CompleteRelease();
                }
            }
        }

        public void Dispatch()
        {
            if (State == PlatformState.Initializing || State == PlatformState.Ready)
            {
                DispatchCore();
            }
        }

        public void Release()
        {
            if (State == PlatformState.Uninitialized || State == PlatformState.Releasing) return;
            releaseInProgress = true;
            try { Session.BeginRelease(); }
            finally
            {
                try { Social?.Release(); Cloud?.Release(); ReleaseCore(); }
                finally
                {
                    releaseInProgress = false;
                    if (!initializationPending) Session.CompleteRelease();
                }
            }
        }

        protected abstract UniTask InitializeCoreAsync(CancellationToken token);
        protected virtual void DispatchCore() { }
        protected abstract void ReleaseCore();
    }
}

