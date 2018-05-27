using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RovOperatorInterface.Communication
{
    class RovNotConnectedException : InvalidOperationException
    {
        public RovNotConnectedException() : base("An attempt was made to perform an operation on an ROV connector while no active connection was available.")
        {
        }
    }
}
