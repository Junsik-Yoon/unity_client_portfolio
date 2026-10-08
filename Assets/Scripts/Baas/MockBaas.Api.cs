using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Backend
{
    public sealed partial class MockBaas
    {
        // 요청 생성 → 공통 실행 → 응답/오류 처리를 재구성한다.
        // SDK/HTTP 호출 대신 지연 뒤 고정 샘플 데이터를 반환한다.
        private readonly Func<int, CancellationToken, UniTask> apiDelay;
        public bool SimulateApiFailure { get; set; }
        public bool SimulateApiTimeout { get; set; }
        public int ApiTimeoutMilliseconds { get; set; } = 2000;

        public MockBaas() : this((ms, token) => UniTask.Delay(ms, ignoreTimeScale: true, cancellationToken: token)) { }
        internal MockBaas(Func<int, CancellationToken, UniTask> apiDelay)
        { this.apiDelay = apiDelay ?? throw new ArgumentNullException(nameof(apiDelay)); }

        public UniTask<LeaderboardResponse> GetLeaderboardAsync(BaasSession session, LeaderboardRequest request, CancellationToken token)
            => ExecuteFunctionAsync("GetLeaderboard", session, () => BuildLeaderboard(request), token);

        public UniTask<InventoryResponse> GetInventoryItemsAsync(BaasSession session, InventoryRequest request, CancellationToken token)
            => ExecuteFunctionAsync("GetInventoryItems", session, () => BuildInventory(session, request), token);

        private async UniTask<T> ExecuteFunctionAsync<T>(string api, BaasSession session, Func<T> response, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (session == null || !session.IsActive)
                throw new BaasApiException(BaasErrorCode.NotAuthenticated, api, "백엔드 로그인이 필요합니다.");
            if (ApiTimeoutMilliseconds <= 0)
                throw new BaasApiException(BaasErrorCode.InvalidRequest, api, "시간 제한은 0보다 커야 합니다.");
            var fail = SimulateApiFailure;
            var delay = SimulateApiTimeout ? int.MaxValue : 500;
            using (var timeout = new CancellationTokenSource())
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, session.LifetimeToken, timeout.Token))
            {
                timeout.CancelAfter(ApiTimeoutMilliseconds);
                try
                {
                    await apiDelay(delay, linked.Token);
                    linked.Token.ThrowIfCancellationRequested();
                    if (fail)
                        throw new BaasApiException(BaasErrorCode.ServiceUnavailable, api, "Mock 서버가 요청을 처리하지 못했습니다. 다시 시도하세요.");
                    return response();
                }
                catch (OperationCanceledException)
                {
                    // 로그아웃/호출자 취소와 서버 시간 초과를 구분한다.
                    if (timeout.IsCancellationRequested && !token.IsCancellationRequested && session.IsActive)
                        throw new BaasApiException(BaasErrorCode.Timeout, api, "Mock 서버 응답 시간이 초과되었습니다.");
                    throw;
                }
            }
        }

        private static LeaderboardResponse BuildLeaderboard(LeaderboardRequest request)
        {
            if (request == null || request.StatisticName != "sample.score" || request.StartPosition < 0 || request.Count < 1 || request.Count > 100)
                throw new BaasApiException(BaasErrorCode.InvalidRequest, "GetLeaderboard", "sample.score / 시작 위치 0 이상 / 개수 1~100을 사용하세요.");
            var entries = new List<LeaderboardEntry>();
            // 서버에 12명의 순위가 있다고 가정
            for (var i = request.StartPosition; i < 12 && entries.Count < request.Count; i++)
                entries.Add(new LeaderboardEntry(i, "sample-player-" + i, "샘플 선수 " + (i + 1), 1200 - i * 75));
            return new LeaderboardResponse(entries);
        }

        private static InventoryResponse BuildInventory(BaasSession session, InventoryRequest request)
        {
            const string api = "GetInventoryItems";
            if (request == null || request.Count < 1 || request.Count > 100)
                throw new BaasApiException(BaasErrorCode.InvalidRequest, api, "페이지 크기는 1~100이어야 합니다.");

            var prefix = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(session.PlayerId)) + ":inventory:";
            var start = 0;
            if (!string.IsNullOrEmpty(request.ContinuationToken) &&
                (!request.ContinuationToken.StartsWith(prefix, StringComparison.Ordinal) ||
                 !int.TryParse(request.ContinuationToken.Substring(prefix.Length), out start) || start < 1 || start >= 5))
                throw new BaasApiException(BaasErrorCode.InvalidRequest, api, "인벤토리 페이지 토큰이 올바르지 않습니다.");
            var items = new[] {
                new InventoryItem("outfit.basic", "기본 의상", 1),
                new InventoryItem("board.blue", "파란 보드", 1),
                new InventoryItem("emote.wave", "인사 이모트", 1),
                new InventoryItem("ticket.reward", "보상 티켓", 3),
                new InventoryItem("currency.coin", "코인", 500)
            };
            var page = new List<InventoryItem>();
            var end = Math.Min(items.Length, start + request.Count);
            for (var i = start; i < end; i++) page.Add(items[i]);
            return new InventoryResponse(page, end < items.Length ? prefix + end : null);
        }
    }
}
