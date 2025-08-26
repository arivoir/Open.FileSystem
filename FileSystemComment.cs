using System;

namespace Open.FileSystem
{
    public class FileSystemComment
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public FileSystemPerson From { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}
