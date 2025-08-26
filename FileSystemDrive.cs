namespace Open.FileSystem
{
    public class FileSystemDrive
    {
        public FileSystemDrive(long? usedSize, long? totalSize, long? maxUploadSize)
        {
            UsedSize = usedSize;
            TotalSize = totalSize;
            MaxUploadSize = maxUploadSize;
        }

        public long? UsedSize { get; private set; }

        public long? TotalSize { get; private set; }

        public long? MaxUploadSize { get; private set; }

        public long? AvailableSize
        {
            get
            {
                if (UsedSize.HasValue && TotalSize.HasValue)
                {
                    return TotalSize.Value - UsedSize.Value;
                }
                return null;
            }
        }
    }
}
