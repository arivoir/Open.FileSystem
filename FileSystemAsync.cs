using C1.DataCollection;
using Open.IO;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystem
{
    /// <summary>
    /// Implements a file system which provides an asynchronous api to the visual components.
    /// </summary>
    public abstract class FileSystemAsync : IFileSystemAsync, ISearchExtension
    {
        #region fields

        protected WeakDictionary<string, IDataCollection<FileSystemDirectory>> DirListCache = new WeakDictionary<string, IDataCollection<FileSystemDirectory>>();
        protected WeakDictionary<string, IDataCollection<FileSystemFile>> FileListCache = new WeakDictionary<string, IDataCollection<FileSystemFile>>();
        protected WeakDictionary<string, FileSystemDirectory> DirectoryCache = new WeakDictionary<string, FileSystemDirectory>();
        protected WeakDictionary<string, FileSystemFile> FileCache = new WeakDictionary<string, FileSystemFile>();

        #endregion

        #region object model

        public virtual string Name
        {
            get
            {
                return "";
            }
        }

        protected virtual bool ShowCountInDirectories
        {
            get
            {
                return false;
            }
        }

        public virtual Task<string> GetTrashId(string relativeDirId, CancellationToken cancellationToken)
        {
            return Task.FromResult((string)null);
        }

        #endregion

        #region resolve names

        protected virtual DirPathMode DirPathMode
        {
            get
            {
                return DirPathMode.FullPathAsId;
            }
        }
        protected virtual UniqueFileNameMode UniqueFileNameMode
        {
            get
            {
                if (DirPathMode == DirPathMode.FullPathAsId)
                {
                    return UniqueFileNameMode.FileId;
                }
                else
                {
                    return UniqueFileNameMode.DirName_FileName;
                }
            }
        }

        protected virtual bool IsFileNameExtensionRequired
        {
            get
            {
                return false;
            }
        }

        public virtual string GetDirectoryId(string parentDirId, string dirLocalId)
        {
            if (DirPathMode == DirPathMode.FullPathAsId)
            {
                return Path.Combine(parentDirId, dirLocalId);
            }
            else
            {
                return dirLocalId;
            }
        }

        public virtual string GetFileId(string parentDirId, string fileLocalId)
        {
            if (DirPathMode == DirPathMode.FullPathAsId)
            {
                return Path.Combine(parentDirId, fileLocalId);
            }
            else
            {
                return fileLocalId;
            }
        }

        public virtual async Task<string> GetDirectoryParentIdAsync(string dirId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dirId))
                return null;

            if (DirPathMode == DirPathMode.FullPathAsId)
            {
                return Path.GetParentPath(dirId);
            }
            else
            {
                var directory = await GetDirectoryAsync(dirId, false, cancellationToken);
                return GetDirectoryParentId(directory);
            }
        }

        public virtual async Task<string> GetFileParentIdAsync(string fileId, CancellationToken cancellationToken)
        {
            if (DirPathMode == DirPathMode.FullPathAsId)
            {
                return Path.GetParentPath(fileId);
            }
            else
            {
                var file = await GetFileAsync(fileId, false, cancellationToken);
                return GetFileParentId(file);
            }
        }

        protected virtual string GetDirectoryParentId(FileSystemDirectory directory)
        {
            throw new NotImplementedException();
        }

        protected virtual string GetFileParentId(FileSystemFile file)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<string> GetFullPathAsync(string dirId, CancellationToken cancellationToken)
        {
            if (DirPathMode == DirPathMode.FullPathAsId)
            {
                return dirId;
            }
            else
            {
                string fullPath = "";
                do
                {
                    fullPath = Path.Combine(dirId, fullPath);
                    dirId = await GetDirectoryParentIdAsync(dirId, cancellationToken);
                }
                while (!string.IsNullOrWhiteSpace(dirId));
                return fullPath;
            }
        }

        public virtual async Task<string> GetFullFilePathAsync(string fileId, CancellationToken cancellationToken)
        {
            if (DirPathMode == DirPathMode.FullPathAsId)
            {
                return fileId;
            }
            else
            {
                var parentId = await GetFileParentIdAsync(fileId, cancellationToken);
                var parentFullPath = await GetFullPathAsync(parentId, cancellationToken);
                return Path.Combine(parentFullPath, fileId);
            }
        }

        public virtual Task<string> GetUniqueDirectoryPathAsync(string dirId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Path.GetValidPath(dirId));
        }

        private string GetUniqueFileName(FileSystemFile file)
        {
            string fileName;
            if (UniqueFileNameMode.UseName())
            {
                fileName = file.Name;
            }
            else
            {
                fileName = file.Id;
            }
            if (UniqueFileNameMode.NeedExtension() && !Path.HasExtension(fileName))
            {
                fileName += MimeType.GetExtensionsFromContentType(file.ContentType).FirstOrDefault();
            }
            return Path.GetValidPathSegment(fileName);
        }

        public virtual async Task<string> GetUniqueFilePathAsync(string fileId, CancellationToken cancellationToken)
        {
            if (UniqueFileNameMode.FileIdIsPath())
            {
                return Path.GetValidPath(fileId);
            }
            string dirFullPath = "";
            if (UniqueFileNameMode.NeedDir())
            {
                var parentId = await GetFileParentIdAsync(fileId, cancellationToken);
                dirFullPath = await GetUniqueDirectoryPathAsync(parentId, cancellationToken);
            }
            var file = await GetFileAsync(fileId, false, cancellationToken);
            var fileName = GetUniqueFileName(file);
            return Path.Combine(dirFullPath, fileName);
        }

        #endregion

        #region load/unload

        private SemaphoreSlim _checkAccesSemaphore = new SemaphoreSlim(1);
        public async Task<bool> CheckAccessAsync(string dirId, bool promptIfNecessary, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            try
            {
                await _checkAccesSemaphore.WaitAsync();
                return await CheckAccessAsyncOverride(dirId, promptIfNecessary, cancellationToken);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
            finally { _checkAccesSemaphore.Release(); }
        }

        public async Task InvalidateAccessAsync(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            try
            {
                await _checkAccesSemaphore.WaitAsync();
                await InvalidateAccessAsyncOverride(dirId, cancellationToken);
            }
            finally { _checkAccesSemaphore.Release(); }
        }

        protected virtual Task<bool> CheckAccessAsyncOverride(string dirId, bool promptIfNecessary, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        protected virtual Task InvalidateAccessAsyncOverride(string dirId, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region get info

        protected virtual bool CacheDirectoriesMetadata(string dirId)
        {
            return true;
        }

        protected virtual bool CacheFilesMetadata(string dirId)
        {
            return true;
        }

        public Task<FileSystemDrive> GetDriveAsync(CancellationToken cancellationToken)
        {
            return GetDriveAsyncOverride(cancellationToken);
        }

        protected virtual Task<FileSystemDrive> GetDriveAsyncOverride(CancellationToken cancellationToken)
        {
            return Task.FromResult((FileSystemDrive)null);
        }

        public async Task<bool> ExistsDirectoryAsync(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);

            if (string.IsNullOrWhiteSpace(dirId))
                return true;

            var dir = await GetDirectoryAsync(dirId, false, cancellationToken);
            if (dir != null)
                return true;

            return false;
        }

        //protected virtual async Task<bool> ExistsDirectoryAsyncOverride(string dirId, CancellationToken cancellationToken)
        //{
        //    throw new NotImplementedException();
        //}

        public async Task<IDataCollection<FileSystemDirectory>> GetDirectoriesAsync(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            IDataCollection<FileSystemDirectory> directories;
            if (DirListCache.TryGetValue(dirId, out directories))
                return new ReadOnlyCollectionView<FileSystemDirectory>(directories);
            try
            {

                directories = await GetDirectoriesAsyncOverride(dirId, cancellationToken);
                WatchDirectories(dirId, directories);
                return new ReadOnlyCollectionView<FileSystemDirectory>(directories);
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        public async Task<IDataCollection<FileSystemFile>> GetFilesAsync(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            IDataCollection<FileSystemFile> files;
            if (FileListCache.TryGetValue(dirId, out files))
                return new ReadOnlyCollectionView<FileSystemFile>(files);
            try
            {
                files = await GetFilesAsyncOverride(dirId, cancellationToken);
                WatchFiles(dirId, files);
                return new ReadOnlyCollectionView<FileSystemFile>(files);
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<IDataCollection<FileSystemDirectory>> GetDirectoriesAsyncOverride(string dirId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IDataCollection<FileSystemDirectory>>(new List<FileSystemDirectory>().AsDataCollection());
        }

        protected virtual Task<IDataCollection<FileSystemFile>> GetFilesAsyncOverride(string dirId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IDataCollection<FileSystemFile>>(new List<FileSystemFile>().AsDataCollection());
        }

        public async Task<FileSystemDirectory> GetDirectoryAsync(string dirId, bool full, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            try
            {
                FileSystemDirectory dir = null;
                if (!full)
                {
                    if (DirectoryCache.TryGetValue(dirId, out dir))
                        return dir;
                }
                dir = await GetDirectoryAsyncOverride(dirId, full, cancellationToken);
                if (dir == null && DirectoryCache.TryGetValue(dirId, out dir))
                    return dir;
                if (dir == null && DirPathMode == DirPathMode.FullPathAsId)
                {
                    try
                    {
                        var parentDirId = await GetDirectoryParentIdAsync(dirId, cancellationToken);
                        var parentSubDirectories = await GetDirectoriesAsync(parentDirId, cancellationToken);
                        await parentSubDirectories.LoadAsync();
                        dir = parentSubDirectories.FirstOrDefault(d => Path.Combine(parentDirId, d.Id) == dirId);
                    }
                    catch { }
                }
                if (dir != null)
                {
                    DirectoryCache[dirId] = dir;
                }
                return dir;
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<FileSystemDirectory> GetDirectoryAsyncOverride(string dirId, bool full, CancellationToken cancellationToken)
        {
            return Task.FromResult((FileSystemDirectory)null);
        }

        public async Task<FileSystemFile> GetFileAsync(string fileId, bool full, CancellationToken cancellationToken)
        {
            fileId = Path.NormalizePath(fileId);
            try
            {
                FileSystemFile file;
                if (!full)
                {
                    if (FileCache.TryGetValue(fileId, out file))
                        return file;
                }
                file = await GetFileAsyncOverride(fileId, full, cancellationToken);
                if (file == null && FileCache.TryGetValue(fileId, out file))
                    return file;
                //if (file == null && DirPathMode == DirPathMode.FullPathAsId)
                //{
                //    try
                //    {
                //        var parentDirId = await GetFileParentIdAsync(fileId, cancellationToken);
                //        var parentDirFiles = await GetFilesAsync(parentDirId, cancellationToken);
                //        await parentDirFiles.LoadAsync();
                //        file = parentDirFiles.FirstOrDefault(f => Path.Combine(parentDirId, f.Id) == fileId);
                //    }
                //    catch { }
                //}
                if (file != null)
                    FileCache[fileId] = file;
                return file;
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<FileSystemFile> GetFileAsyncOverride(string fileId, bool full, CancellationToken cancellationToken)
        {
            return Task.FromResult((FileSystemFile)null);
        }

        public Task<Stream> GetDirectoryIconAsync(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            return GetDirectoryIconAsyncOverride(dirId, cancellationToken);
        }

        protected virtual Task<Stream> GetDirectoryIconAsyncOverride(string dirId, CancellationToken cancellationToken)
        {
            return Task.FromResult((Stream)null);
        }

        public Task<bool> CanOpenDirectoryThumbnailAsync(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            return CanOpenDirectoryThumbnailAsyncOverride(dirId, cancellationToken);
        }

        protected virtual async Task<bool> CanOpenDirectoryThumbnailAsyncOverride(string dirId, CancellationToken cancellationToken)
        {
            var directory = await GetDirectoryAsync(dirId, false, cancellationToken);
            if (directory == null)
                return false;
            return directory != null && directory.HasThumbnail;
        }
        public async Task<Stream> OpenDirectoryThumbnailAsync(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            try
            {
                return await OpenDirectoryThumbnailAsyncOverride(dirId, cancellationToken);
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual async Task<Stream> OpenDirectoryThumbnailAsyncOverride(string dirId, CancellationToken cancellationToken)
        {
            var dir = await GetDirectoryAsync(dirId, false, cancellationToken);
            if (dir != null && !string.IsNullOrWhiteSpace(dir.Thumbnail))
            {
                var thumbnail = dir.Thumbnail;
                var client = new HttpClient();
                var response = await client.GetAsync(new Uri(thumbnail), cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return new StreamWithLength(await response.Content.ReadAsStreamAsync(), response.Content.Headers.ContentLength);
                }
                else
                {
                    if (MimeType.Parse(response.Content.Headers.ContentType.MediaType).Type != "image")
                    {
                        var message = response.EnsureSuccessStatusCode();
                    }
                    throw new ImageException(await response.Content.ReadAsByteArrayAsync());
                }
            }
            return null;
        }

        public Task<bool> CanOpenFileThumbnailAsync(string fileId, CancellationToken cancellationToken)
        {
            fileId = Path.NormalizePath(fileId);
            return CanOpenFileThumbnailAsyncOverride(fileId, cancellationToken);
        }

        protected virtual async Task<bool> CanOpenFileThumbnailAsyncOverride(string fileId, CancellationToken cancellationToken)
        {
            var file = await GetFileAsync(fileId, false, cancellationToken);
            if (file == null)
                return false;
            return file != null && file.HasThumbnail;
        }

        public async Task<Stream> OpenFileThumbnailAsync(string fileId, CancellationToken cancellationToken)
        {
            fileId = Path.NormalizePath(fileId);
            try
            {
                return await OpenFileThumbnailAsyncOverride(fileId, cancellationToken);
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual async Task<Stream> OpenFileThumbnailAsyncOverride(string fileId, CancellationToken cancellationToken)
        {
            var file = await GetFileAsync(fileId, false, cancellationToken);
            if (file != null && !string.IsNullOrWhiteSpace(file.Thumbnail))
            {
                var thumbnail = file.Thumbnail;
                var client = new HttpClient();
                var response = await client.GetAsync(new Uri(thumbnail), cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return new StreamWithLength(await response.Content.ReadAsStreamAsync(), response.Content.Headers.ContentLength);
                }
                else
                {
                    if (MimeType.Parse(response.Content.Headers.ContentType.MediaType).Type != "image")
                    {
                        var message = response.EnsureSuccessStatusCode();
                    }
                    throw new ImageException(await response.Content.ReadAsByteArrayAsync());
                }
            }
            return null;
        }

        public Task<bool> CanGetDirectoryLinkAsync(string directoryId, CancellationToken cancellationToken)
        {
            directoryId = Path.NormalizePath(directoryId);
            return CanGetDirectoryLinkAsyncOverride(directoryId, cancellationToken);
        }

        public virtual async Task<bool> CanGetDirectoryLinkAsyncOverride(string dirId, CancellationToken cancellationToken)
        {
            var directory = await GetDirectoryAsync(dirId, false, cancellationToken);
            return directory != null && directory.Link != null;
        }

        public async Task<Uri> GetDirectoryLinkAsync(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            return await GetDirectoryLinkAsyncOverride(dirId, cancellationToken);
        }

        protected virtual async Task<Uri> GetDirectoryLinkAsyncOverride(string dirId, CancellationToken cancellationToken)
        {
            var directory = await GetDirectoryAsync(dirId, false, cancellationToken);
            return directory.Link;
        }

        public Task<bool> CanGetFileLinkAsync(string fileId, CancellationToken cancellationToken)
        {
            fileId = Path.NormalizePath(fileId);
            return CanGetFileLinkAsyncOverride(fileId, cancellationToken);
        }

        public virtual async Task<bool> CanGetFileLinkAsyncOverride(string fileId, CancellationToken cancellationToken)
        {
            var file = await GetFileAsync(fileId, false, cancellationToken);
            return file != null && file.Link != null;
        }

        public Task<Uri> GetFileLinkAsync(string fileId, CancellationToken cancellationToken)
        {
            fileId = Path.NormalizePath(fileId);
            return GetFileLinkAsyncOverride(fileId, cancellationToken);
        }

        protected virtual async Task<Uri> GetFileLinkAsyncOverride(string fileId, CancellationToken cancellationToken)
        {
            var file = await GetFileAsync(fileId, false, cancellationToken);
            return file.Link;
        }

        #endregion

        #region shift

        public Task<bool> CanShiftDirectory(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            return CanShiftDirectoryOverride(dirId, cancellationToken);
        }

        protected virtual Task<bool> CanShiftDirectoryOverride(string dirId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public virtual Task<bool> ShiftDirectoryAsync(string dirId, int targetIndex)
        {
            return Task.FromException<bool>(new NotImplementedException());
        }

        #endregion

        #region download

        public Task<bool> CanOpenFileAsync(string fileId, CancellationToken cancellationToken)
        {
            fileId = Path.NormalizePath(fileId);
            return CanOpenFileAsyncOverride(fileId, cancellationToken);
        }

        protected virtual Task<bool> CanOpenFileAsyncOverride(string fileId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public async Task<Stream> OpenFileAsync(string fileId, CancellationToken cancellationToken)
        {
            fileId = Path.NormalizePath(fileId);
            try
            {
                if (!await CanOpenFileAsync(fileId, cancellationToken))
                    throw new ArgumentException("Can not download the file with the specified fileId");
                return await OpenFileReadAsyncOverride(fileId, cancellationToken);
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<Stream> OpenFileReadAsyncOverride(string fileId, CancellationToken cancellationToken)
        {
            return Task.FromException<Stream>(new NotImplementedException());
        }

        #endregion

        #region upload

        public string[] GetAcceptedFileTypes(string dirId, bool includeSubDirectories)
        {
            dirId = Path.NormalizePath(dirId);
            return GetAcceptedFileTypesOverride(dirId, includeSubDirectories);
        }

        protected virtual string[] GetAcceptedFileTypesOverride(string dirId, bool includeSubDirectories)
        {
            return new string[] { "*/*" };
        }

        public Task<bool> CanWriteFileAsync(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            return CanWriteFileAsyncOverride(dirId, cancellationToken);
        }

        protected virtual Task<bool> CanWriteFileAsyncOverride(string dirId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public async Task<FileSystemFile> WriteFileAsync(string dirId, FileSystemFile file, Stream fileStream, IProgress<StreamProgress> progress, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            try
            {
                if (!await CanWriteFileAsync(dirId, cancellationToken))
                    throw new ArgumentException("Can not upload file to the specified dirId");
                if (IsFileNameExtensionRequired)
                {
                    if (!Path.HasExtension(file.Name) && !string.IsNullOrWhiteSpace(file.ContentType))
                    {
                        var extension = MimeType.GetExtensionsFromContentType(file.ContentType).FirstOrDefault();
                        if (extension != null)
                            file.Name = file.Name + extension;
                    }
                }

                var uploadedFile = await WriteFileAsyncOverride(dirId, file, fileStream, progress, cancellationToken);

                await AddOrReplaceFileToCache(dirId, uploadedFile);
                if (ShowCountInDirectories)
                {
                    await UpdateDirectoryAsync(dirId, 1, cancellationToken);
                }
                return uploadedFile;
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<FileSystemFile> WriteFileAsyncOverride(string dirId, FileSystemFile file, Stream fileStream, IProgress<StreamProgress> progress, CancellationToken cancellationToken)
        {
            return Task.FromException<FileSystemFile>(new NotImplementedException());
        }

        #endregion

        #region create

        public Task<bool> CanCreateDirectory(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            return CanCreateDirectoryOverride(dirId, cancellationToken);
        }

        protected virtual Task<bool> CanCreateDirectoryOverride(string dirId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public async Task<FileSystemDirectory> CreateDirectoryAsync(string dirId, FileSystemDirectory directory, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            try
            {
                if (!await CanCreateDirectory(dirId, cancellationToken))
                    throw new ArgumentException("Can not create the folder at the specified dirId");

                var newDirectory = await CreateDirectoryAsyncOverride(dirId, directory, cancellationToken);

                await AddDirToCache(dirId, newDirectory);
                if (ShowCountInDirectories)
                {
                    await UpdateDirectoryAsync(dirId, 1, cancellationToken);
                }
                return newDirectory;
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<FileSystemDirectory> CreateDirectoryAsyncOverride(string dirId, FileSystemDirectory directory, CancellationToken cancellationToken)
        {
            return Task.FromException<FileSystemDirectory>(new NotImplementedException());
        }

        #endregion

        #region update

        public Task<bool> CanUpdateDirectory(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            return CanUpdateDirectoryOverride(dirId, cancellationToken);
        }

        protected virtual Task<bool> CanUpdateDirectoryOverride(string dirId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> CanUpdateFile(string fileId, CancellationToken cancellationToken)
        {
            fileId = Path.NormalizePath(fileId);
            return CanUpdateFileOverride(fileId, cancellationToken);
        }

        protected virtual Task<bool> CanUpdateFileOverride(string fileId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public async Task<FileSystemDirectory> UpdateDirectoryAsync(string dirId, FileSystemDirectory directory, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            try
            {
                if (!await CanUpdateDirectory(dirId, cancellationToken))
                    throw new ArgumentException("Can not update the folder with the specified dirId");
                var parentId = await GetDirectoryParentIdAsync(dirId, cancellationToken);
                var originalDirectory = await GetDirectoryAsync(dirId, false, cancellationToken);

                var updatedDirectory = await UpdateDirectoryAsyncOverride(dirId, directory, cancellationToken);

                await ReplaceDirectoryInCache(parentId, originalDirectory, updatedDirectory);

                return updatedDirectory;
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<FileSystemDirectory> UpdateDirectoryAsyncOverride(string dirId, FileSystemDirectory directory, CancellationToken cancellationToken)
        {
            return Task.FromException<FileSystemDirectory>(new NotImplementedException());
        }

        public async Task<FileSystemFile> UpdateFileAsync(string fileId, FileSystemFile file, CancellationToken cancellationToken)
        {
            fileId = Path.NormalizePath(fileId);
            try
            {
                if (!await CanUpdateFile(fileId, cancellationToken))
                    throw new ArgumentException("Can not update the file with the specified fileId");
                var parentId = await GetFileParentIdAsync(fileId, cancellationToken);
                var originalFile = await GetFileAsync(fileId, false, cancellationToken);

                var updatedFile = await UpdateFileAsyncOverride(fileId, file, cancellationToken);

                await ReplaceFileInCache(parentId, originalFile, updatedFile);

                return updatedFile;
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<FileSystemFile> UpdateFileAsyncOverride(string fileId, FileSystemFile file, CancellationToken cancellationToken)
        {
            return Task.FromException<FileSystemFile>(new NotImplementedException());
        }

        #endregion

        #region copy

        public Task<bool> CanCopyDirectory(string sourceDirId, string targetDirId, CancellationToken cancellationToken)
        {
            sourceDirId = Path.NormalizePath(sourceDirId);
            targetDirId = Path.NormalizePath(targetDirId);
            return CanCopyDirectoryOverride(sourceDirId, targetDirId, cancellationToken);
        }

        protected virtual Task<bool> CanCopyDirectoryOverride(string sourceDirId, string targetDirId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> CanCopyFile(string sourceFileId, string targetDirId, CancellationToken cancellationToken)
        {
            sourceFileId = Path.NormalizePath(sourceFileId);
            targetDirId = Path.NormalizePath(targetDirId);
            return CanCopyFileOverride(sourceFileId, targetDirId, cancellationToken);
        }

        protected virtual Task<bool> CanCopyFileOverride(string sourceFileId, string targetDirId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public async Task<FileSystemDirectory> CopyDirectoryAsync(string sourceDirId, string targetDirId, FileSystemDirectory dir, CancellationToken cancellationToken)
        {
            sourceDirId = Path.NormalizePath(sourceDirId);
            targetDirId = Path.NormalizePath(targetDirId);
            try
            {
                if (!await CanCopyDirectory(sourceDirId, targetDirId, cancellationToken))
                    throw new ArgumentException("Can not copy the directory with the specified sourceDirId and targetDirId");

                var copiedDir = await CopyDirectoryAsyncOverride(sourceDirId, targetDirId, dir, cancellationToken);

                await AddDirToCache(targetDirId, copiedDir);
                if (ShowCountInDirectories)
                {
                    await UpdateDirectoryAsync(targetDirId, 1, cancellationToken);
                }
                return copiedDir;
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<FileSystemDirectory> CopyDirectoryAsyncOverride(string sourceDirId, string targetDirId, FileSystemDirectory dir, CancellationToken cancellationToken)
        {
            return Task.FromException<FileSystemDirectory>(new NotImplementedException());
        }

        public async Task<FileSystemFile> CopyFileAsync(string sourceFileId, string targetDirId, FileSystemFile file, CancellationToken cancellationToken)
        {
            sourceFileId = Path.NormalizePath(sourceFileId);
            targetDirId = Path.NormalizePath(targetDirId);
            try
            {
                if (!await CanCopyFile(sourceFileId, targetDirId, cancellationToken))
                    throw new ArgumentException("Can not copy the file with the specified sourceFileId and targetDirId");

                var copiedFile = await CopyFileAsyncOverride(sourceFileId, targetDirId, file, cancellationToken);

                await AddFileToCache(targetDirId, copiedFile);
                if (ShowCountInDirectories)
                {
                    await UpdateDirectoryAsync(targetDirId, 1, cancellationToken);
                }
                return copiedFile;
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<FileSystemFile> CopyFileAsyncOverride(string sourceFileId, string targetDirId, FileSystemFile file, CancellationToken cancellationToken)
        {
            return Task.FromException<FileSystemFile>(new NotImplementedException());
        }

        #endregion

        #region move

        public async Task<bool> CanMoveFile(string sourceFileId, string targetDirId, CancellationToken cancellationToken)
        {
            sourceFileId = Path.NormalizePath(sourceFileId);
            targetDirId = Path.NormalizePath(targetDirId);
            var parentId = await GetFileParentIdAsync(sourceFileId, cancellationToken);
            if (parentId == targetDirId)
                return false;
            return await CanMoveFileOverride(sourceFileId, targetDirId, cancellationToken);
        }

        protected virtual Task<bool> CanMoveFileOverride(string sourceFileId, string targetDirId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public async Task<bool> CanMoveDirectory(string sourceDirId, string targetDirId, CancellationToken cancellationToken)
        {
            var parentId = await GetDirectoryParentIdAsync(sourceDirId, cancellationToken);
            if (parentId != null && parentId == targetDirId)
                return false;
            return await CanMoveDirectoryOverride(sourceDirId, targetDirId, cancellationToken);
        }

        protected virtual Task<bool> CanMoveDirectoryOverride(string sourceDirId, string targetDirId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public async Task<FileSystemDirectory> MoveDirectoryAsync(string sourceDirId, string targetDirId, FileSystemDirectory directory, CancellationToken cancellationToken)
        {
            sourceDirId = Path.NormalizePath(sourceDirId);
            targetDirId = Path.NormalizePath(targetDirId);
            try
            {
                if (!await CanMoveDirectory(sourceDirId, targetDirId, cancellationToken))
                    throw new ArgumentException("Can not move the folder with the specified sourceDirId and targetDirId");
                var sourceParentId = await GetDirectoryParentIdAsync(sourceDirId, cancellationToken);

                var movedDir = await MoveDirectoryAsyncOverride(sourceDirId, targetDirId, directory, cancellationToken);

                await RemoveDirFromCache(sourceParentId, sourceDirId);
                await AddDirToCache(targetDirId, movedDir);
                if (ShowCountInDirectories)
                {
                    await UpdateDirectoryAsync(sourceParentId, -1, cancellationToken);
                    await UpdateDirectoryAsync(targetDirId, 1, cancellationToken);
                }

                return movedDir;
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<FileSystemDirectory> MoveDirectoryAsyncOverride(string sourceDirId, string targetDirId, FileSystemDirectory directory, CancellationToken cancellationToken)
        {
            return Task.FromException<FileSystemDirectory>(new NotImplementedException());
        }

        public async Task<FileSystemFile> MoveFileAsync(string sourceFileId, string targetDirId, FileSystemFile file, CancellationToken cancellationToken)
        {
            sourceFileId = Path.NormalizePath(sourceFileId);
            targetDirId = Path.NormalizePath(targetDirId);
            try
            {
                if (!await CanMoveFile(sourceFileId, targetDirId, cancellationToken))
                    throw new ArgumentException("Can not move the file with the specified sourceFileId to the directory with the specified targetDirId");
                var sourceParentId = await GetFileParentIdAsync(sourceFileId, cancellationToken);

                var movedFile = await MoveFileAsyncOverride(sourceFileId, targetDirId, file, cancellationToken);

                await RemoveFileFromCache(sourceParentId, sourceFileId);
                await AddFileToCache(targetDirId, movedFile);
                if (ShowCountInDirectories)
                {
                    await UpdateDirectoryAsync(sourceParentId, -1, cancellationToken);
                    await UpdateDirectoryAsync(targetDirId, 1, cancellationToken);
                }

                return movedFile;
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<FileSystemFile> MoveFileAsyncOverride(string sourceFileId, string targetDirId, FileSystemFile file, CancellationToken cancellationToken)
        {
            return Task.FromException<FileSystemFile>(new NotImplementedException());
        }

        #endregion

        #region delete

        public Task<bool> CanDeleteDirectory(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            return CanDeleteDirectoryOverride(dirId, cancellationToken);
        }

        protected virtual Task<bool> CanDeleteDirectoryOverride(string dirId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<bool> CanDeleteFile(string fileId, CancellationToken cancellationToken)
        {
            fileId = Path.NormalizePath(fileId);
            return CanDeleteFileOverride(fileId, cancellationToken);
        }

        protected virtual Task<bool> CanDeleteFileOverride(string fileId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public async Task<FileSystemDirectory> DeleteDirectoryAsync(string dirId, bool sendToTrash, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            try
            {
                if (!await CanDeleteDirectory(dirId, cancellationToken))
                    throw new ArgumentException("Can not delete the folder with the specified dirId");
                var parentId = await GetDirectoryParentIdAsync(dirId, cancellationToken);
                string trashId = null;
                if (sendToTrash)
                    trashId = await GetTrashId(dirId, cancellationToken);

                var directory = await DeleteDirectoryAsyncOverride(dirId, sendToTrash, cancellationToken);

                await RemoveDirFromCache(parentId, dirId);
                if (directory != null && trashId != null)
                {
                    await AddDirToCache(trashId, directory);
                }
                if (ShowCountInDirectories && !string.IsNullOrWhiteSpace(parentId))
                {
                    await UpdateDirectoryAsync(parentId, -1, cancellationToken);
                }

                return directory;
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<FileSystemDirectory> DeleteDirectoryAsyncOverride(string dirId, bool sendToTrash, CancellationToken cancellationToken)
        {
            return Task.FromException<FileSystemDirectory>(new NotImplementedException());
        }

        public async Task<FileSystemFile> DeleteFileAsync(string fileId, bool sendToTrash, CancellationToken cancellationToken)
        {
            fileId = Path.NormalizePath(fileId);
            try
            {
                if (!await CanDeleteFile(fileId, cancellationToken))
                    throw new ArgumentException("Can not delete the file with the specified fileId");
                var parentId = await GetFileParentIdAsync(fileId, cancellationToken);
                string trashId = null;
                if (sendToTrash)
                    trashId = await GetTrashId(parentId, cancellationToken);

                var deletedFile = await DeleteFileAsyncOverride(fileId, sendToTrash, cancellationToken);

                await RemoveFileFromCache(parentId, fileId);
                if (deletedFile != null && trashId != null)
                {
                    await AddFileToCache(trashId, deletedFile);
                }
                if (ShowCountInDirectories && !string.IsNullOrWhiteSpace(parentId))
                {
                    await UpdateDirectoryAsync(parentId, -1, cancellationToken);
                }

                return deletedFile;
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<FileSystemFile> DeleteFileAsyncOverride(string fileId, bool sendToTrash, CancellationToken cancellationToken)
        {
            return Task.FromException<FileSystemFile>(new NotImplementedException());
        }

        #endregion

        #region search

        public Task<bool> CanSearchAsync(string dirId, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            return CanSearchAsyncOverride(dirId, cancellationToken);
        }

        protected virtual Task<bool> CanSearchAsyncOverride(string dirId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public async Task<IDataCollection<FileSystemSearchItem>> SearchAsync(string dirId, string query, CancellationToken cancellationToken)
        {
            dirId = Path.NormalizePath(dirId);
            try
            {
                if (!await CanSearchAsync(dirId, cancellationToken))
                    throw new ArgumentException("Can not search at the specified dirId");

                var searchResult = await SearchAsyncOverride(dirId, query, cancellationToken);
                WatchSearchList(searchResult);
                return new ReadOnlyCollectionView<FileSystemSearchItem>(searchResult);
            }
            catch (Exception exc) { throw await ProcessExceptionAsync(exc); }
        }

        protected virtual Task<IDataCollection<FileSystemSearchItem>> SearchAsyncOverride(string dirId, string query, CancellationToken cancellationToken)
        {
            return Task.FromException<IDataCollection<FileSystemSearchItem>>(new NotImplementedException());
        }

        //protected virtual async Task<IAsyncList<FileSystemSearchItem>> SearchAsyncOverride(string dirId, string query, CancellationToken cancellationToken)
        //{
        //    throw new NotImplementedException();
        //}

        #endregion

        #region refresh

        public event EventHandler<RefreshedEventArgs> Refreshed;

        public async Task RefreshAsync(string dirId = null)
        {
            if (dirId != null)
                dirId = Path.NormalizePath(dirId);
            await RefreshAsyncOverride(dirId);
            if (dirId == null)
            {
                DirListCache.Clear();
                FileListCache.Clear();
                FileCache.Clear();
                DirectoryCache.Clear();
            }
            else
            {
                DirListCache.Remove(dirId);
                FileListCache.Remove(dirId);
                foreach (var entry in FileCache.ToArray())
                {
                    var parentDirId = await GetFileParentIdAsync(entry.Key, CancellationToken.None);
                    if (parentDirId == dirId)
                    {
                        FileCache.Remove(entry.Key);
                    }
                }
                foreach (var entry in DirectoryCache.ToArray())
                {
                    var parentDirId = await GetDirectoryParentIdAsync(entry.Key, CancellationToken.None);
                    if (parentDirId == dirId)
                    {
                        DirectoryCache.Remove(entry.Key);
                    }
                }
            }
            var e = new RefreshedEventArgs(dirId);
            OnRefreshed(e);
            await e.WaitDeferralsAsync();
            //var refreshDirs = RefreshDirectories(dirId);
            //var refreshFiles = RefreshFiles(dirId);
            //await new List<Task> { refreshDirs, refreshFiles }.WhenAll();
        }

        protected virtual Task RefreshAsyncOverride(string dirId = null)
        {
            return Task.FromResult(false);
        }

        protected void OnRefreshed(RefreshedEventArgs e)
        {
            if (Refreshed != null)
            {
                Refreshed(this, e);
            }
        }

        #endregion

        #region exception management

        protected virtual Task<Exception> ProcessExceptionAsync(Exception exc)
        {
            return Task.FromResult(ProcessOAuthException(exc));
        }

        protected static Exception ProcessOAuthException(Exception exc)
        {
            if (exc.Message == "invalid_grant" || exc.Message == "unauthorized_client" || exc.Message == "expired_token")
            {
                return new AccessDeniedException();
            }
            return exc;
        }

        #endregion

        #region cache management

        private void WatchFiles(string dirId, IDataCollection<FileSystemFile> files)
        {
            FileListCache[dirId] = files;
            files.CollectionChanged += (s, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (FileSystemFile file in e.NewItems)
                        {
                            var fileId = GetFileId(dirId, file.Id);
                            FileCache[fileId] = file;
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (FileSystemFile file in e.OldItems)
                        {
                            var fileId = GetFileId(dirId, file.Id);
                            FileCache.Remove(fileId);
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        foreach (FileSystemFile file in e.OldItems)
                        {
                            if (file != null)
                            {
                                var fileId = GetFileId(dirId, file.Id);
                                FileCache.Remove(fileId);
                            }
                        }
                        foreach (FileSystemFile file in e.NewItems)
                        {
                            if (file != null)
                            {
                                var fileId = GetFileId(dirId, file.Id);
                                FileCache[fileId] = file;
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        foreach (var info in files.GetLoadedItems())
                        {
                            if (info.Item != null)
                            {
                                var fileId = GetFileId(dirId, info.Item.Id);
                                FileCache[fileId] = info.Item;
                            }
                        }
                        break;
                    default:
                        break;
                }
            };
            foreach (var info in files.GetLoadedItems())
            {
                if (info.Item != null)
                {
                    var fileId = GetFileId(dirId, info.Item.Id);
                    FileCache[fileId] = info.Item;
                }
            }
        }

        private void WatchDirectories(string dirId, IDataCollection<FileSystemDirectory> directories)
        {
            DirListCache[dirId] = directories;
            directories.CollectionChanged += (s, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (FileSystemDirectory dir in e.NewItems)
                        {
                            var parentDirId = GetDirectoryId(dirId, dir.Id);
                            DirectoryCache[parentDirId] = dir;
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (FileSystemDirectory dir in e.OldItems)
                        {
                            var parentDirId = GetDirectoryId(dirId, dir.Id);
                            DirectoryCache.Remove(parentDirId);
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        foreach (FileSystemDirectory dir in e.OldItems)
                        {
                            if (dir != null)
                            {
                                var parentDirId = GetDirectoryId(dirId, dir.Id);
                                DirectoryCache.Remove(parentDirId);
                            }
                        }
                        foreach (FileSystemDirectory dir in e.NewItems)
                        {
                            if (dir != null)
                            {
                                var parentDirId = GetDirectoryId(dirId, dir.Id);
                                DirectoryCache[parentDirId] = dir;
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        foreach (var subDirInfo in directories.GetLoadedItems())
                        {
                            var subDirId = GetDirectoryId(dirId, subDirInfo.Item.Id);
                            DirectoryCache[subDirId] = subDirInfo.Item;
                        }
                        break;
                    default:
                        break;
                }
            };
            foreach (var subDirInfo in directories.GetLoadedItems())
            {
                var subDirId = GetDirectoryId(dirId, subDirInfo.Item.Id);
                DirectoryCache[subDirId] = subDirInfo.Item;
            }
        }

        private void WatchSearchList(IDataCollection<FileSystemSearchItem> list)
        {
            list.CollectionChanged += (s, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (FileSystemSearchItem item in e.NewItems)
                        {
                            if (item.Item.IsDirectory)
                            {
                                var dir = item.Item as FileSystemDirectory;
                                var dirId = GetDirectoryId(item.DirectoryId, dir.Id);
                                DirectoryCache[dirId] = dir;
                            }
                            else
                            {
                                var file = item.Item as FileSystemFile;
                                var fileId = GetFileId(item.DirectoryId, file.Id);
                                FileCache[fileId] = file;
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (FileSystemSearchItem item in e.OldItems)
                        {
                            if (item.Item.IsDirectory)
                            {
                                var dir = item.Item as FileSystemDirectory;
                                var dirId = GetDirectoryId(item.DirectoryId, dir.Id);
                                DirectoryCache.Remove(dirId);
                            }
                            else
                            {
                                var file = item.Item as FileSystemFile;
                                var fileId = GetFileId(item.DirectoryId, file.Id);
                                FileCache.Remove(fileId);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        foreach (var info in list.GetLoadedItems())
                        {
                            if (info.Item != null)
                            {
                                if (info.Item.Item.IsDirectory)
                                {
                                    var dir = info.Item.Item as FileSystemDirectory;
                                    var dirId = GetDirectoryId(info.Item.DirectoryId, dir.Id);
                                    DirectoryCache[dirId] = dir;
                                }
                                else
                                {
                                    var file = info.Item.Item as FileSystemFile;
                                    var fileId = GetFileId(info.Item.DirectoryId, file.Id);
                                    FileCache[fileId] = file;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            };
            foreach (var info in list.GetLoadedItems())
            {
                if (info.Item != null)
                {
                    if (info.Item.Item.IsDirectory)
                    {
                        var dir = info.Item.Item as FileSystemDirectory;
                        var dirId = GetDirectoryId(info.Item.DirectoryId, dir.Id);
                        DirectoryCache[dirId] = dir;
                    }
                    else
                    {
                        var file = info.Item.Item as FileSystemFile;
                        var fileId = GetFileId(info.Item.DirectoryId, file.Id);
                        FileCache[fileId] = file;
                    }
                }
            }
        }

        private Task AddDirToCache(string parentDirId, FileSystemDirectory dir)
        {
            var dirId = GetDirectoryId(parentDirId, dir.Id);
            DirectoryCache[dirId] = dir;
            IDataCollection<FileSystemDirectory> targetDirSubdirectories;
            if (DirListCache.TryGetValue(parentDirId, out targetDirSubdirectories))
            {
                return targetDirSubdirectories.AddAsync(dir);
            }
            return Task.FromResult(true);
        }

        protected Task AddFileToCache(string dirId, FileSystemFile file)
        {
            var fileId = GetFileId(dirId, file.Id);
            FileCache[fileId] = file;
            IDataCollection<FileSystemFile> files;
            if (FileListCache.TryGetValue(dirId, out files))
            {
                return files.AddAsync(file);
            }
            return Task.FromResult(true);
        }

        protected Task AddOrReplaceFileToCache(string dirId, FileSystemFile file)
        {
            var fileId = GetFileId(dirId, file.Id);
            FileCache[fileId] = file;
            IDataCollection<FileSystemFile> files;
            if (FileListCache.TryGetValue(dirId, out files))
            {
                var oldFile = files.GetLoadedItems().FirstOrDefault(info => GetFileId(dirId, info.Item.Id) == fileId);
                if (oldFile != null)
                    return files.ReplaceAsync(oldFile.Index, file);
                else
                    return files.AddAsync(file);
            }
            return Task.FromResult(true);
        }

        protected Task ReplaceDirectoryInCache(string parentId, FileSystemDirectory dir, FileSystemDirectory updatedDirectory)
        {
            var oldDirId = GetDirectoryId(parentId, dir.Id);
            var newDirId = GetDirectoryId(parentId, updatedDirectory.Id);
            if (oldDirId != newDirId)
            {
                DirectoryCache.Remove(oldDirId);
            }
            DirectoryCache[newDirId] = updatedDirectory;
            IDataCollection<FileSystemDirectory> directories;
            if (DirListCache.TryGetValue(parentId, out directories))
            {
                var item = directories.GetLoadedItems().FirstOrDefault(i => i.Item.Id == dir.Id);
                if (item != null)
                    return directories.ReplaceAsync(item.Index, updatedDirectory);
            }
            return Task.FromResult(true);
        }


        protected Task ReplaceFileInCache(string parentId, FileSystemFile file, FileSystemFile updatedFile)
        {
            var oldFileId = GetFileId(parentId, file.Id);
            var newFileId = GetFileId(parentId, updatedFile.Id);
            if (oldFileId != newFileId)
            {
                FileCache.Remove(oldFileId);
            }
            FileCache[newFileId] = updatedFile;
            IDataCollection<FileSystemFile> files;
            if (FileListCache.TryGetValue(parentId, out files))
            {
                var index = files.IndexOf(file);
                if (index >= 0)
                    return files.ReplaceAsync(index, updatedFile);
            }
            return Task.FromResult(true);
        }

        protected Task RemoveDirFromCache(string parentId, string dirId)
        {
            DirectoryCache.Remove(dirId);
            IDataCollection<FileSystemDirectory> dirs;
            if (DirListCache.TryGetValue(parentId, out dirs))
            {
                var dir = dirs.GetLoadedItems().Where(info => GetDirectoryId(parentId, info.Item.Id) == dirId).FirstOrDefault();
                if (dir != null)
                    return dirs.RemoveAsync(dir.Index);
            }
            return Task.FromResult(true);
        }

        protected Task RemoveFileFromCache(string parentId, string fileId)
        {
            FileCache.Remove(fileId);
            IDataCollection<FileSystemFile> files;
            if (FileListCache.TryGetValue(parentId, out files))
            {
                var file = files.GetLoadedItems().FirstOrDefault(info => GetFileId(parentId, info.Item.Id) == fileId);
                if (file != null)
                    return files.RemoveAsync(file.Index);
            }
            return Task.FromResult(true);
        }

        private async Task UpdateDirectoryAsync(string dirId, int delta, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(dirId))
            {
                var parentDirId = await GetDirectoryParentIdAsync(dirId, cancellationToken);
                //var updatedDir = await GetDirectoryTransactionAsync(dirId, false, cancellationToken);
                FileSystemDirectory updatedDir = null;
                if (DirectoryCache.TryGetValue(dirId, out updatedDir))
                {
                    if (updatedDir.Count.HasValue)
                        updatedDir.Count = updatedDir.Count.Value + delta;
                    IDataCollection<FileSystemDirectory> dirs;
                    if (DirListCache.TryGetValue(parentDirId, out dirs))
                    {
                        var oldDir = dirs.Where(d => GetDirectoryId(parentDirId, d.Id) == dirId).FirstOrDefault();
                        var index = dirs.IndexOf(oldDir);
                        if (index >= 0)
                            await dirs.ReplaceAsync(index, updatedDir);
                    }
                }
            }
        }

        #endregion
    }

    public enum DirPathMode
    {
        FullPathAsId,
        DirIdAsId,
    }

    public enum UniqueFileNameMode
    {
        FileId,
        DirName_FileName,
        DirName_FileId_Extension,
    }

    public static class UniqueFileNameModeEx
    {
        public static bool FileIdIsPath(this UniqueFileNameMode mode)
        {
            return mode == UniqueFileNameMode.FileId;
        }
        public static bool NeedDir(this UniqueFileNameMode mode)
        {
            return mode != UniqueFileNameMode.FileId;
        }
        public static bool UseName(this UniqueFileNameMode mode)
        {
            return mode == UniqueFileNameMode.DirName_FileName;
        }
        public static bool NeedExtension(this UniqueFileNameMode mode)
        {
            return mode == UniqueFileNameMode.DirName_FileId_Extension;
        }
    }

    public static class FileSystemAsyncEx
    {
        public static async Task<bool> IsSubDirectory(this IFileSystemAsync fileSystem, string dirId1, string dirId2, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dirId2))
                return true;
            while (!string.IsNullOrWhiteSpace(dirId1))
            {
                if (dirId1 == dirId2)
                    return true;
                dirId1 = await fileSystem.GetDirectoryParentIdAsync(dirId1, cancellationToken);
            }
            return false;
        }
    }
}
