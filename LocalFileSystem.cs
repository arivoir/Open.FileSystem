using C1.DataCollection;
using Open.IO;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystem
{
    public class LocalFileSystem : FileSystemAsync
    {
        private IFileSystemStorage _storage;

        public LocalFileSystem(IFileSystemStorage storage)
        {
            _storage = storage;
        }

        protected override bool CacheDirectoriesMetadata(string dirId)
        {
            return false;
        }

        protected override bool CacheFilesMetadata(string dirId)
        {
            return false;
        }

        protected override Task<bool> CheckAccessAsyncOverride(string dirId, bool promptIfNecessary, CancellationToken cancellationToken)
        {
            return _storage.CheckAccessAsync(cancellationToken);
        }

        protected override async Task<IDataCollection<FileSystemDirectory>> GetDirectoriesAsyncOverride(string dirId, System.Threading.CancellationToken cancellationToken)
        {
            var directories = await _storage.GetDirectoriesAsync(dirId);
            return directories.Select(dirPath => { var fileName = Path.GetFileName(dirPath); return new FileSystemDirectory(fileName, fileName, true); }).AsDataCollection();
        }

        protected override async Task<IDataCollection<FileSystemFile>> GetFilesAsyncOverride(string dirId, System.Threading.CancellationToken cancellationToken)
        {
            var files = await _storage.GetFilesAsync(dirId);
            return files.Select(filePath => { var fileName = Path.GetFileName(filePath); return new FileSystemFile(fileName, fileName, true); }).AsDataCollection();
        }

        protected override Task<bool> CanCreateDirectoryOverride(string dirId, System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult<bool>(true);
        }

        protected override async Task<FileSystemDirectory> CreateDirectoryAsyncOverride(string dirId, FileSystemDirectory item, System.Threading.CancellationToken cancellationToken)
        {
            var fileName = item.Name;
            await _storage.CreateDirectoryAsync(Path.Combine(dirId, fileName));
            return new FileSystemDirectory(fileName, fileName, true);
        }

        protected override Task<bool> CanWriteFileAsyncOverride(string dirId, CancellationToken cancellationToken)
        {
            return Task.FromResult<bool>(true);
        }

        protected override async Task<FileSystemFile> WriteFileAsyncOverride(string dirId, FileSystemFile file, Stream fileStream, IProgress<StreamProgress> progress, CancellationToken cancellationToken)
        {
            var filePath = Path.Combine(dirId, file.Name);
            var fileInfo = await _storage.CreateFileAsync(filePath);
            Stream stream = null;
            try
            {
                stream = await fileInfo.OpenWriteAsync();
                await fileStream.CopyToAsync(stream, progress: progress, cancellationToken: cancellationToken);
                return new FileSystem.FileSystemFile(fileInfo.Name, fileInfo.Name, true);
            }
            catch
            {
                stream?.Dispose();
                await _storage.DeleteFileAsync(filePath);
                throw;
            }
            finally
            {
                stream?.Dispose();
            }
        }
    }
}
