using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
