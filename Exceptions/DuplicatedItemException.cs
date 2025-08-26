using System;

namespace Open.FileSystemAsync
{
    public class DuplicatedItemException : Exception
    {
        public DuplicatedItemException(string message) :
            base(message)
        {

        }
    }
}
