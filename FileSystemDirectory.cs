
namespace Open.FileSystem
{
    public class FileSystemDirectory : FileSystemItem
    {
        public FileSystemDirectory()
            : base(true)
        {
        }

        public FileSystemDirectory(string id, string name, bool isReadOnly)
            : this()
        {
            Id = id;
            Name = name;
            IsReadOnly = isReadOnly;
        }

        public int? Count { get; internal protected set; }

        public bool IsSpecial { get; protected set; }
    }
}
