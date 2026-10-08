using System;
using System.Text;

namespace Portfolio.DemoSupport
{
    internal static class MockAuthCode
    {
        public static string Create(string provider, string userId)
            => "demo|" + provider + "|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(userId))
                + "|" + Guid.NewGuid().ToString("N");

        public static string Validate(string provider, string code)
        {
            var parts = (code ?? string.Empty).Split('|');
            if (parts.Length != 4 || parts[0] != "demo" || parts[1] != provider ||
                !Guid.TryParseExact(parts[3], "N", out _))
                throw new InvalidOperationException("Mock 인증 코드 또는 플랫폼이 올바르지 않습니다.");
            string userId;
            try { userId = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2])); }
            catch (FormatException) { throw new InvalidOperationException("Mock 인증 코드 형식 오류"); }
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidOperationException("Mock 인증 코드에 사용자 정보가 없습니다.");
            return userId;
        }
    }
}
