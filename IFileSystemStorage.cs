using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystem
{
    public interface IFileSystemStorage
    {
        Task<bool> CheckAccessAsync(CancellationToken cancellationToken);
        Task CreateDirectoryAsync(string path);
        Task<IFileInfo> CreateFileAsync(string path);
        Task<IFileInfo> TryGetFileAsync(string path);
        Task<bool> DeleteDirectoryAsync(string path);
        Task<bool> DeleteFileAsync(string path);
        Task<bool> CopyFileAsync(string origianlPath, string targetPath);
        Task<bool> MoveDirectoryAsync(string origianlPath, string updatedPath);
        Task<bool> MoveFileAsync(string origianlPath, string updatedPath);
        Task<long> GetFolderSizeAsync(string path);
        Task<long> GetFreeSpaceAsync();
        Task<IReadOnlyList<string>> GetDirectoriesAsync(string path);
        Task<IReadOnlyList<string>> GetFilesAsync(string path);
    }
}
