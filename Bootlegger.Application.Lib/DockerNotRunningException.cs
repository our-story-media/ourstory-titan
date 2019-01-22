using System;
using System.Runtime.Serialization;

namespace Bootlegger.App.Lib
{
    [Serializable]
    internal class DockerNotRunningException : Exception
    {
        public DockerNotRunningException()
        {
        }

        public DockerNotRunningException(string message) : base(message)
        {
        }

        public DockerNotRunningException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DockerNotRunningException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}