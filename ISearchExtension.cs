using C1.DataCollection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystem
{
    public interface ISearchExtension
    {
        Task<bool> CanSearchAsync(string dirId, CancellationToken cancellationToken);
        Task<IDataCollection<FileSystemSearchItem>> SearchAsync(string dirId, string query, CancellationToken cancellationToken);
    }

    public class FileSystemSearchItem
    {
        public string DirectoryId { get; set; }
        public FileSystemItem Item { get; set; }
    }

    public static class ISearchExtensionEx
    {
        public static Task<bool> CanSearchAsync(this IFileSystemAsync fileSystem, string dirId, CancellationToken cancellationToken)
        {
            if (fileSystem is ISearchExtension searchExtension)
                return searchExtension.CanSearchAsync(dirId, cancellationToken);
            return Task.FromResult(false);
        }

        public static Task<IDataCollection<FileSystemSearchItem>> SearchAsync(this IFileSystemAsync fileSystem, string dirId, string query, CancellationToken cancellationToken)
        {
            if (fileSystem is ISearchExtension searchExtension)
                return searchExtension.SearchAsync(dirId, query, cancellationToken);
            throw new NotImplementedException();
        }
    }
}
