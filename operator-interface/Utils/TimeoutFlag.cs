using System;

namespace RovOperatorInterface.Utils
{
    public class TimeoutFlag
    {
        private DateTime? LastActivation = null;
        private readonly TimeSpan TimeoutPeriod;

        public TimeoutFlag(TimeSpan timeoutPeriod)
        {
            this.TimeoutPeriod = timeoutPeriod;
        }

        public void RegisterUpdate() => LastActivation = DateTime.UtcNow;
        public bool IsTimedOut() => LastActivation != null && DateTime.UtcNow - LastActivation > TimeoutPeriod;
    }
}
