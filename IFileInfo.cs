using System.IO;
using System.Threading.Tasks;

namespace Open.FileSystem
{
    public interface IFileInfo
    {
        string Name { get; }
        string ContentType { get; }
        long? Size { get; }
        Task<Stream> OpenWriteAsync();

        Task<byte[]> ReadAsBufferAsync();
        Task<Stream> OpenSequentialReadAsync();
    }
}
