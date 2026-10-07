using System.Threading;
using Cysharp.Threading.Tasks;

namespace Portfolio.Platforms
{
    public sealed class NoPlatform : PlatformBase
    {
        public NoPlatform()
        {
            Social = new NoPlatformSocial(Session);
            Cloud = new NoPlatformCloud(Session);
            Stats = new NoPlatformStats(Session);
            Restriction = new NoPlatformRestriction(Session);
        }

        protected override UniTask InitializeCoreAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
        }

        protected override void ReleaseCore() { }
    }
}

