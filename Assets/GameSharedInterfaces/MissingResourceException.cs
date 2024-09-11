using System;

namespace GameSharedInterfaces
{
    public class MissingResourceException : Exception
    {
        public MissingResourceException() : base("Missing resource")
        {
        }

        public MissingResourceException(string location)
            : base($"Missing resource at {location}")
        {
        }

        public MissingResourceException(string location, Exception inner)
            : base($"Missing resource at {location}", inner)
        {
        }
    }
}