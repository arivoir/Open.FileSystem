using System;

namespace Open.FileSystem
{
    public class DuplicatedItemException : Exception
    {
        public DuplicatedItemException(string message) :
            base(message)
        {

        }
    }
}
