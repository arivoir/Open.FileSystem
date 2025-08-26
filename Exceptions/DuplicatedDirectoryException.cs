
namespace Open.FileSystem
{
    public class DuplicatedDirectoryException : DuplicatedItemException
    {
        public DuplicatedDirectoryException(string message) :
            base(message)
        {

        }
    }
}
