using System;

namespace GameLogic
{
    public class ServerOnlyException : InvalidOperationException
    {
        public ServerOnlyException() : base("Client attempted to run serverside code")
        {
        }

        public ServerOnlyException(string message)
            : base(message)
        {
        }

        public ServerOnlyException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}