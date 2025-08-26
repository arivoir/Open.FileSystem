using C1.DataCollection;
using System;
using System.Threading.Tasks;

namespace Open.FileSystem
{
    public interface ISocialExtension
    {
        Task<FileSystemPerson> GetCurrentUserAsync(string subPath);
        /*Comments*/
        event EventHandler CommentsChanged;
        bool CanAddComment(string fileId);
        Task<IDataCollection<FileSystemComment>> GetCommentsAsync(string fileId);
        Task AddCommentAsync(string fileId, string message);

        /*Thumbs Up*/
        bool CanThumbUp(string fileId);
        Task AddThumbUp(string fileId);
        Task RemoveThumbUp(string fileId);
        Task<IDataCollection<FileSystemPerson>> GetThumbsUpAsync(string fileId);
    }
}
