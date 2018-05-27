using System;
using System.Runtime.Serialization;

namespace RovOperatorInterface.Communication
{
    class RovSendOperationFailedException : Exception
    {
        private const string DefaultMessage = "An error occurred while attempting to send a message to the ROV.";
        public RovSendOperationFailedException() : base(DefaultMessage)
        {
        }

        public RovSendOperationFailedException(string message) : base(message)
        {
        }

        public RovSendOperationFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public RovSendOperationFailedException(Exception innerException) : base(DefaultMessage, innerException)
        {

        }

        protected RovSendOperationFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
