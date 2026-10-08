using Portfolio.DemoSupport;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Backend
{
    // GBaas의 플랫폼별 로그인 호출 흐름 예시
    public sealed class MockBaas : IBaasAuth
    {
        public bool SimulateLoginFailure { get; set; }

        public async UniTask<BaasSession> LoginWithPlatformAsync(string provider, string authCode, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (provider != "MockPlatform1" && provider != "MockPlatform2" && provider != "MockPlatform3")
                throw new NotSupportedException("백엔드 인증을 지원하지 않는 플랫폼입니다.");
            var userId = MockAuthCode.Validate(provider, authCode);
            await UniTask.Delay(700, ignoreTimeScale: true, cancellationToken: token);
            token.ThrowIfCancellationRequested();
            if (SimulateLoginFailure)
                throw new InvalidOperationException("MockBaas 로그인 실패 — 다시 시도하세요.");

            return new BaasSession("baas:" + provider + ":" + userId, provider, userId);
        }
    }
}
