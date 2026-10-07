using System;

namespace Portfolio.Platforms
{
    public sealed class PlatformUser
    {
        public string Id { get; }
        public string DisplayName { get; }
        public bool IsOnline { get; }

        public PlatformUser(string id, string displayName, bool isOnline)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("사용자 ID가 필요합니다.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("표시 이름이 필요합니다.", nameof(displayName));
            Id = id;
            DisplayName = displayName;
            IsOnline = isOnline;
        }
    }
}
