namespace Open.FileSystem
{
    public class FileSystemFile : FileSystemItem
    {
        protected string _contentType = null;

        protected FileSystemFile()
            : base(false)
        {
        }

        public FileSystemFile(string id, string name, string contentType, bool isReadOnly)
            : this()
        {
            Id = id;
            Name = name;
            _contentType = contentType;
            IsReadOnly = isReadOnly;
        }

        public FileSystemFile(string id, string name, bool isReadOnly)
            : this()
        {
            Id = id;
            Name = name;
            IsReadOnly = isReadOnly;
        }

        /// <summary>
        /// Gets the mime type of the content of the file.
        /// </summary>
        public virtual string ContentType
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_contentType))
                    return _contentType;
                return MimeType.GetContentTypeFromExtension(Path.GetExtension(Name)) ?? "";
            }
        }
    }
}
