using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Platforms
{
    public abstract class PlatformBaseSocial
    {
        protected readonly PlatformSession Session;
        public PlatformUser Me { get; private set; }
        public IReadOnlyList<PlatformUser> Friends { get; private set; } = Array.Empty<PlatformUser>();

        protected PlatformBaseSocial(PlatformSession session) => Session = session;

        internal async UniTask InitializeAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var _me = await LoadLocalUserCoreAsync(token);
            token.ThrowIfCancellationRequested();
            Me = _me;
            Friends = Array.Empty<PlatformUser>();
        }

        public async UniTask<PlatformUser> GetLocalUserAsync(CancellationToken token)
        {
            using (var scope = Session.CreateRequestScope(token))
            {
                var _me = await LoadLocalUserCoreAsync(scope.Token);
                scope.Token.ThrowIfCancellationRequested();
                Me = _me;
                return _me;
            }
        }

        public async UniTask RefreshFriendListAsync(CancellationToken token)
        {
            using (var scope = Session.CreateRequestScope(token))
            {
                var friends = await LoadFriendsCoreAsync(scope.Token);
                scope.Token.ThrowIfCancellationRequested();
                Friends = new List<PlatformUser>(friends).AsReadOnly();
            }
        }

        public async UniTask ShowProfileAsync(string platformId, CancellationToken token)
        {
            using (var scope = Session.CreateRequestScope(token))
            {
                if (string.IsNullOrWhiteSpace(platformId)) throw new ArgumentException("사용자 식별자가 필요합니다.");
                await ShowProfileCoreAsync(platformId, scope.Token);
                scope.Token.ThrowIfCancellationRequested();
            }
        }

        internal void Release()
        {
            Me = null;
            Friends = Array.Empty<PlatformUser>();
            ReleaseCore();
        }

        protected abstract UniTask<PlatformUser> LoadLocalUserCoreAsync(CancellationToken token);
        protected abstract UniTask<List<PlatformUser>> LoadFriendsCoreAsync(CancellationToken token);
        protected abstract UniTask ShowProfileCoreAsync(string platformId, CancellationToken token);
        protected virtual void ReleaseCore() { }
    }
}
