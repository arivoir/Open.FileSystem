using System;
using System.Linq;
using System.Reflection;

namespace Open.FileSystem
{
    public class FileSystemItem
    {
        #region fields

        private string _name;
        private string _permissions;
        private GeoPosition _where;

        #endregion

        #region initialization

        public FileSystemItem(bool isDirectory)
        {
            IsDirectory = isDirectory;
            IsReadOnly = false;
        }

        #endregion

        #region object model

        /// <summary>
        /// Gets a value indicating whether this item is a directory.
        /// </summary>
        public bool IsDirectory { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this item can be modified.
        /// </summary>
        public bool IsReadOnly { get; protected set; }

        /// <summary>
        /// Gets the local identifier.
        /// </summary>
        public string Id { get; protected internal set; }

        /// <summary>
        /// Gets or sets the name of the file or directory.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (IsReadOnly) throw new InvalidOperationException();
                _name = value;
            }
        }

        public string Permissions
        {
            get
            {
                return _permissions;
            }
            set
            {
                if (IsReadOnly) throw new InvalidOperationException();
                _permissions = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this file or directory has a thumbnail.
        /// </summary>
        public virtual bool HasThumbnail
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Thumbnail);
            }
        }

        /// <summary>
        /// Gets or sets the thumbnail corresponding to the file or directory.
        /// </summary>
        public string Thumbnail { get; protected set; }

        /// <summary>
        /// Gets or sets the size of the file or directory.
        /// </summary>
        public long? Size { get; protected set; }

        public Uri Link { get; protected set; }

        public GeoPosition Where
        {
            get
            {
                return _where;
            }
            set
            {
                if (IsReadOnly) throw new InvalidOperationException();
                _where = value;
            }
        }

        public FileSystemPerson Owner { get; protected set; }
        public DateTime? CreatedDate { get; protected set; }
        public DateTime? ModifiedDate { get; protected set; }

        #endregion

        #region implementation

        public static void Copy(FileSystemItem source, FileSystemItem target)
        {
            var sourceType = source.GetType();
            var targetType = target.GetType();
            foreach (var sourceProperty in sourceType.GetRuntimeProperties())
            {
                try
                {
                    if (sourceProperty.Name == "IsReadOnly")
                        continue;
                    var targetProperty = targetType.GetRuntimeProperties().FirstOrDefault(p => p.Name == sourceProperty.Name);
                    if (targetProperty != null && targetProperty.CanWrite)
                        targetProperty.SetValue(target, sourceProperty.GetValue(source));
                }
                catch { }
            }
        }

        #endregion
    }
}
