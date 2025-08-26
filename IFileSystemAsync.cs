using C1.DataCollection;
using Open.IO;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystem
{
    public interface IFileSystemAsync
    {
        /***********General***********/
        event EventHandler<RefreshedEventArgs> Refreshed;
        Task RefreshAsync(string dirId = null);

        Task<bool> CheckAccessAsync(string dirId, bool promptForUserInteraction, CancellationToken cancellationToken);
        Task InvalidateAccessAsync(string dirId, CancellationToken cancellationToken);

        Task<FileSystemDrive> GetDriveAsync(CancellationToken cancellationToken);
        Task<string> GetTrashId(string relativeDirId, CancellationToken cancellationToken);

        /***********Names resolution***********/
        Task<string> GetFullPathAsync(string dirId, CancellationToken cancellationToken);
        string GetDirectoryId(string parentDirId, string dirName);
        Task<string> GetFullFilePathAsync(string dirId, CancellationToken cancellationToken);
        string GetFileId(string parentDirId, string fileName);
        Task<string> GetDirectoryParentIdAsync(string dirId, CancellationToken cancellationToken);
        Task<string> GetFileParentIdAsync(string fileId, CancellationToken cancellationToken);
        /***********Queries***********/
        Task<bool> ExistsDirectoryAsync(string dirId, CancellationToken cancellationToken);
        Task<IDataCollection<FileSystemDirectory>> GetDirectoriesAsync(string dirId, CancellationToken cancellationToken);
        Task<IDataCollection<FileSystemFile>> GetFilesAsync(string dirId, CancellationToken cancellationToken);
        Task<Stream> GetDirectoryIconAsync(string dirId, CancellationToken cancellationToken);
        Task<FileSystemDirectory> GetDirectoryAsync(string dirId, bool full, CancellationToken cancellationToken);
        Task<FileSystemFile> GetFileAsync(string fileId, bool full, CancellationToken cancellationToken);
        Task<Uri> GetDirectoryLinkAsync(string dirId, CancellationToken cancellationToken);
        Task<Uri> GetFileLinkAsync(string fileId, CancellationToken cancellationToken);

        /***********Configuration***********/
        string[] GetAcceptedFileTypes(string dirId, bool includeSubDirectories);
        Task<bool> CanCopyDirectory(string sourceDirId, string targetDirId, CancellationToken cancellationToken);
        Task<bool> CanCopyFile(string sourceFileId, string targetDirId, CancellationToken cancellationToken);
        Task<bool> CanDeleteDirectory(string dirId, CancellationToken cancellationToken);
        Task<bool> CanDeleteFile(string fileId, CancellationToken cancellationToken);
        Task<bool> CanCreateDirectory(string dirId, CancellationToken cancellationToken);
        Task<bool> CanWriteFileAsync(string dirId, CancellationToken cancellationToken);
        Task<bool> CanOpenFileAsync(string fileId, CancellationToken cancellationToken);
        Task<bool> CanMoveFile(string sourceFileId, string targetDirId, CancellationToken cancellationToken);
        Task<bool> CanMoveDirectory(string sourceDirId, string targetDirId, CancellationToken cancellationToken);
        Task<bool> CanUpdateDirectory(string dirId, CancellationToken cancellationToken);
        Task<bool> CanUpdateFile(string fileId, CancellationToken cancellationToken);
        Task<bool> CanShiftDirectory(string dirId, CancellationToken cancellationToken);
        Task<bool> CanGetDirectoryLinkAsync(string dirId, CancellationToken cancellationToken);
        Task<bool> CanGetFileLinkAsync(string fileId, CancellationToken cancellationToken);
        Task<bool> CanOpenDirectoryThumbnailAsync(string dirId, CancellationToken cancellationToken);
        Task<bool> CanOpenFileThumbnailAsync(string fileId, CancellationToken cancellationToken);

        /***********Modifiers***********/
        Task<Stream> OpenFileAsync(string fileId, CancellationToken cancellationToken);
        Task<Stream> OpenDirectoryThumbnailAsync(string dirId, CancellationToken cancellationToken);
        Task<Stream> OpenFileThumbnailAsync(string fileId, CancellationToken cancellationToken);
        Task<FileSystemFile> WriteFileAsync(string dirId, FileSystemFile file, Stream fileStream, IProgress<StreamProgress> progress, CancellationToken cancellationToken);
        Task<FileSystemDirectory> UpdateDirectoryAsync(string dirId, FileSystemDirectory dir, CancellationToken cancellationToken);
        Task<FileSystemFile> UpdateFileAsync(string fileId, FileSystemFile file, CancellationToken cancellationToken);
        Task<FileSystemDirectory> DeleteDirectoryAsync(string dirId, bool sendToTrash, CancellationToken cancellationToken);
        Task<FileSystemFile> DeleteFileAsync(string fileId, bool sendToTrash, CancellationToken cancellationToken);
        Task<FileSystemDirectory> MoveDirectoryAsync(string sourceDirId, string targetDirId, FileSystemDirectory dir, CancellationToken cancellationToken);
        Task<FileSystemFile> MoveFileAsync(string sourceFileId, string targetDirId, FileSystemFile file, CancellationToken cancellationToken);
        Task<FileSystemDirectory> CopyDirectoryAsync(string sourceDirId, string targetDirId, FileSystemDirectory dir, CancellationToken cancellationToken);
        Task<FileSystemFile> CopyFileAsync(string sourceFileId, string targetDirId, FileSystemFile file, CancellationToken cancellationToken);
        Task<FileSystemDirectory> CreateDirectoryAsync(string dirId, FileSystemDirectory dir, CancellationToken cancellationToken);
        Task<bool> ShiftDirectoryAsync(string dirId, int targetIndex);

    }
}