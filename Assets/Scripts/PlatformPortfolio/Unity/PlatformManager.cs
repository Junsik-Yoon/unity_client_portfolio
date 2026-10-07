using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Portfolio.Platforms
{
    public enum PlatformProvider 
    { 
        [InspectorName("플랫폼 없음")] NoPlatform = 0, 
        [InspectorName("Mock 플랫폼 1")] MockPlatform1 = 1, 
        [InspectorName("Mock 플랫폼 2")] MockPlatform2 = 2, 
        [InspectorName("Mock 플랫폼 3")] MockPlatform3 = 3 
    }

    public sealed class PlatformManager : MonoBehaviour
    {
        private static PlatformManager instance;
        public static PlatformManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var _go = new GameObject(nameof(PlatformManager));
                    instance = _go.AddComponent<PlatformManager>();
                    DontDestroyOnLoad(instance);
                }
                return instance;
            }
        }
        private bool releasing;

        private PlatformBase platform;
        [SerializeField] private PlatformProvider provider = PlatformProvider.MockPlatform1;
        private UniTaskCompletionSource<PlatformBase> initialization;
        private CancellationTokenSource lifetime;
        public PlatformTaskManager RestrictQueue = null;

        public static string BasePath = null;

        // InitializeAsync 이후 호출
        public static PlatformBase Platform
        {
            get
            {
                if (instance == null || !instance.isActiveAndEnabled ||
                    instance.releasing || instance.platform == null || !instance.platform.IsInitialized)
                    throw new InvalidOperationException("플랫폼 초기화 미완료");
                return instance.platform;
            }
        }

        public bool Initialized => platform != null && platform.IsInitialized;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            lifetime = new CancellationTokenSource();
            DontDestroyOnLoad(gameObject);
        }
        private void Update()
        {
            // 플랫폼 sdk가 동작하려면 update루프에서 계속 호출되어야 함
            platform?.Dispatch();
        }
        private void OnDisable()
        {
            if (instance == this)
            {
                ReleasePlatform();
            }
        }
        private void OnApplicationQuit()
        {
            if (instance != this)
            {
                return;
            }
            lifetime?.Cancel();
            ReleasePlatform();
        }
        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }
            try { lifetime?.Cancel(); }
            finally
            {
                ReleasePlatform();
                lifetime?.Dispose();
                lifetime = null;
                instance = null;
            }
        }
        public void InitializeCoreSystem()
        {
            //InitPlayerPrefs();
            SetIOBasePath();
            InitCore();
            RestrictQueue = gameObject.AddComponent<PlatformTaskManager>();
        }
        public async UniTask<PlatformBase> InitializeAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (releasing || !isActiveAndEnabled || lifetime == null || lifetime.IsCancellationRequested)
            {
                throw new InvalidOperationException("플랫폼 매니저를 사용할 수 없습니다.");
            }

            if (Initialized)
            {
                return platform;
            }

            var _completion = initialization;
            if (_completion == null)
            {
                var _created = CreatePlatform();
                platform = _created;
                _completion = new UniTaskCompletionSource<PlatformBase>();
                initialization = _completion;
                InitializeOwnedAsync(_created, _completion).Forget();
            }

            var _ready = await _completion.Task.AttachExternalCancellation(token);
            token.ThrowIfCancellationRequested();
            if (!ReferenceEquals(platform, _ready) || !_ready.IsInitialized)
            {
                throw new InvalidOperationException("대기 중 플랫폼이 종료되었습니다.");
            }
            return _ready;
        }

        private async UniTask InitializeOwnedAsync(PlatformBase target, UniTaskCompletionSource<PlatformBase> completion)
        {
            try
            {
                await target.InitializeAsync(lifetime.Token);
                lifetime.Token.ThrowIfCancellationRequested();
                if (!ReferenceEquals(platform, target))
                {
                    throw new OperationCanceledException("초기화 중 플랫폼이 해제되었습니다.");
                }
                initialization = null;
                completion.TrySetResult(target);
            }
            catch (OperationCanceledException exception)
            {
                CleanupFailedInitialization(target);
                completion.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                CleanupFailedInitialization(target);
                completion.TrySetException(exception);
            }
        }

        private void CleanupFailedInitialization(PlatformBase target)
        {
            if (ReferenceEquals(platform, target))
            {
                platform = null;
            }
            target.Release();
            initialization = null;
        }

        public void SelectProvider(PlatformProvider selected)
        {
            if (releasing || initialization != null || platform != null)
            {
                throw new InvalidOperationException("error on SelectProvider");
            }
            provider = selected;
        }

        private PlatformBase CreatePlatform()
        {
            switch (provider)
            {
                case PlatformProvider.NoPlatform: return new NoPlatform();
                case PlatformProvider.MockPlatform1: return new MockPlatform1();
                case PlatformProvider.MockPlatform2: return new MockPlatform2();
                case PlatformProvider.MockPlatform3: return new MockPlatform3();
                default: throw new NotSupportedException("지원하지 않는 플랫폼");
            }
        }

        public void ReleasePlatform()
        {
            if (releasing)
            {
                return;
            }
            releasing = true;
            var _previous = platform;
            platform = null;
            try { _previous?.Release(); }
            finally { releasing = false; }
        }

        private static void SetIOBasePath()
        {
#if MOCKPLATFORM2 || MOCKPLATFORM3
            BasePath = Application.persistentDataPath;
#else
            BasePath = "";
#endif
        }
        private void InitCore()
        {
#if MOCKPLATFORM2
            // frame 0, awake에서 시도
            Unity.MockPlatform1.IntentSystem.Noti += MockPlatform1.OnIntentNoti;
#endif
        }
    }
}




