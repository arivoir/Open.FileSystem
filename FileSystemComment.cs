using System;

namespace Open.FileSystemAsync
{
    public class FileSystemComment
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public FileSystemPerson From { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}
