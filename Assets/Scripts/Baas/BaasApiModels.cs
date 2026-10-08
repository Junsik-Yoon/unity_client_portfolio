using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Backend
{
    public interface IBaasApi
    {
        UniTask<LeaderboardResponse> GetLeaderboardAsync(BaasSession session, LeaderboardRequest request, CancellationToken token);
        UniTask<InventoryResponse> GetInventoryItemsAsync(BaasSession session, InventoryRequest request, CancellationToken token);
    }

    public sealed class LeaderboardRequest
    {
        public string StatisticName { get; }
        public int StartPosition { get; }
        public int Count { get; }
        public LeaderboardRequest(string statisticName, int startPosition = 0, int count = 5)
        { StatisticName = statisticName; StartPosition = startPosition; Count = count; }
    }
    public sealed class LeaderboardEntry
    {
        public int Position { get; }
        public string PlayerId { get; }
        public string DisplayName { get; }
        public int Value { get; }
        public LeaderboardEntry(int position, string playerId, string displayName, int value)
        { Position = position; PlayerId = playerId; DisplayName = displayName; Value = value; }
    }
    public sealed class LeaderboardResponse
    {
        public IReadOnlyList<LeaderboardEntry> Entries { get; }
        public LeaderboardResponse(List<LeaderboardEntry> entries) { Entries = entries.AsReadOnly(); }
    }
    public sealed class InventoryRequest
    {
        public int Count { get; }
        public string ContinuationToken { get; }
        public InventoryRequest(int count = 2, string continuationToken = null)
        { Count = count; ContinuationToken = continuationToken; }
    }
    public sealed class InventoryItem
    {
        public string ItemId { get; }
        public string DisplayName { get; }
        public int Amount { get; }
        public InventoryItem(string itemId, string displayName, int amount)
        { ItemId = itemId; DisplayName = displayName; Amount = amount; }
    }
    public sealed class InventoryResponse
    {
        public IReadOnlyList<InventoryItem> Items { get; }
        public string ContinuationToken { get; }
        public InventoryResponse(List<InventoryItem> items, string continuationToken)
        { Items = items.AsReadOnly(); ContinuationToken = continuationToken; }
    }
    public enum BaasErrorCode { NotAuthenticated, InvalidRequest, ServiceUnavailable, Timeout }
    public sealed class BaasApiException : Exception
    {
        public BaasErrorCode Code { get; }
        public string ApiName { get; }
        public BaasApiException(BaasErrorCode code, string apiName, string message) : base(message)
        { Code = code; ApiName = apiName; }
    }
}
