using System;

namespace Game_Logic.TriumphAndTragedy.AI
{
    public class BotException : Exception
    {
        public BotException()
        {
        }

        public BotException(string message)
            : base(message)
        {
        }

        public BotException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}