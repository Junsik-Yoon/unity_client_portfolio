using System;
using System.Threading;

namespace Portfolio.Platforms
{
    public enum PlatformState { Uninitialized, Initializing, Ready, Releasing }

    public sealed class PlatformSession
    {
        private CancellationTokenSource lifetime;
        public PlatformState State { get; private set; }

        public CancellationTokenSource CreateRequestScope(CancellationToken callerToken)
        {
            callerToken.ThrowIfCancellationRequested();
            if (State != PlatformState.Ready)
                throw new InvalidOperationException("플랫폼 초기화가 완료되지 않았거나 종료되었습니다.");
            return CancellationTokenSource.CreateLinkedTokenSource(callerToken, lifetime.Token);
        }

        internal CancellationToken BeginInitialization()
        {
            lifetime = new CancellationTokenSource();
            State = PlatformState.Initializing;
            return lifetime.Token;
        }

        internal void MarkReady() => State = PlatformState.Ready;

        internal void BeginRelease()
        {
            State = PlatformState.Releasing;
            lifetime.Cancel();
        }

        internal void CompleteRelease()
        {
            lifetime.Dispose();
            lifetime = null;
            State = PlatformState.Uninitialized;
        }
    }
}
