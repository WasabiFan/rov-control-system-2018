using RovOperatorInterface.Communication;
using System;
using System.Runtime.Serialization;

namespace RovOperatorInterface.Core
{
    [Serializable]
    internal class SerialMessageMalformedException : Exception
    {
        public SerialMessageMalformedException(string message) : base(message)
        {
        }

        public SerialMessageMalformedException(string message, SerialMessage sourceMessageData) : base(message + Environment.NewLine + $"Serialized message: {sourceMessageData.Serialize()}")
        {

        }

        public SerialMessageMalformedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}