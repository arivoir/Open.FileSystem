using System.Collections.Generic;
using System.Linq;

namespace Open.FileSystem
{
    public class MimeType
    {
        #region fields

        private string _type = "";
        private string _subType = "";
        private static Dictionary<string, string[]> _extensionsTable;

        #endregion

        #region initialization

        internal MimeType(string mimeType)
        {
            if (mimeType != null)
            {
                var parts = mimeType.Trim().Split('/');
                if (parts.Length > 0)
                    _type = parts[0].ToLower();
                if (parts.Length > 1)
                    _subType = parts[1].ToLower();
            }
        }

        static MimeType()
        {
            InitilizeExtensionsTable();
        }

        #endregion

        #region object model

        public string Type
        {
            get
            {
                return _type;
            }
        }

        public string SubType
        {
            get
            {
                return _subType;
            }
        }

        #endregion

        #region implementation

        public static implicit operator MimeType(string mimeType)
        {
            return new MimeType(mimeType);
        }

        public static bool operator ==(MimeType a, MimeType b)
        {
            // If both are null, or both are same instance, return true.
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.Type == b.Type && a.SubType == b.SubType;
        }

        //public static bool operator ==(MimeType a, string b)
        //{
        //    return a == new MimeType(b);
        //}

        public static bool operator !=(MimeType c1, MimeType c2)
        {
            return !(c1 == c2);
        }
        //public static bool operator !=(MimeType c1, string c2)
        //{
        //    return !(c1 == c2);
        //}

        public override bool Equals(object obj)
        {
            var mime = obj as MimeType;
            if (mime != (MimeType)null)
            {
                return mime.Type == Type && mime.SubType == SubType;
            }
            var str = obj as string;
            if (str != null)
            {
                return Equals(new MimeType(str));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode() ^ SubType.GetHashCode();
        }

        public override string ToString()
        {
            return !string.IsNullOrWhiteSpace(_type) && !string.IsNullOrWhiteSpace(_subType) ? string.Format("{0}/{1}", _type, _subType) : "";
        }

        public static MimeType Parse(string mimeType)
        {
            return new MimeType(mimeType);
        }

        #endregion

        #region extensions


        private static void InitilizeExtensionsTable()
        {
            //The order matters when trying to get the mimeType from the extension.
            _extensionsTable = new Dictionary<string, string[]>()
            {
                { "image/jpeg", new string[] { ".jpeg", ".jpg" } },
                { "image/gif", new string[] { ".gif" } },
                { "image/bmp", new string[] { ".bmp" } },
                { "image/png", new string[] { ".png" } },
                { "image/tiff", new string[] { ".tiff" } },
                { "image/heif", new string[] { ".heif" } },
                { "image/heic", new string[] { ".heic" } },
                { "image/heif-sequence", new string[] { ".heif" } },
                { "image/heic-sequence", new string[] { ".heic" } },
                { "application/msword", new string[] { ".doc", ".dot" } },
                { "application/vnd.ms-word.document.12", new string[] {".docx" } },
                { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", new string[] { ".docx" } },
                { "application/vnd.openxmlformats-officedocument.wordprocessingml.template", new string[] { ".dotx" } },
                { "application/rtf", new string[] { ".rtf" } },
                { "text/plain", new string[] { ".txt" } },
                { "text/html", new string[] { ".html", ".htm", ".shtml" } },
                { "text/csv", new string[] { ".csv" } },
                { "application/xml", new string[] { ".xml" } },
                { "text/xml", new string[] { ".xml" } },
                { "application/vnd.ms-excel", new string[] { ".xls" } },
                { "application/vnd.ms-excel.12", new string[] { ".xlsx" } },
                { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", new string[] { ".xlsx" } },
                { "application/vnd.openxmlformats-officedocument.spreadsheetml.template", new string[] { ".xltx" } },
                { "application/vnd.ms-powerpoint", new string[] { ".ppt", ".pps", ".pot", "ppa" } },
                { "application/vnd.openxmlformats-officedocument.presentationml.template", new string[] { ".potx" } },
                { "application/vnd.openxmlformats-officedocument.presentationml.slideshow", new string[] { ".ppsx" } },
                { "application/vnd.openxmlformats-officedocument.presentationml.presentation", new string[] { ".pptx" } },
                { "application/vnd.openxmlformats-officedocument.presentationml.slide", new string[] { ".sldx" } },
                { "application/pdf", new string[] { ".pdf" } },
                { "audio/mpeg", new string[] { ".mp3" } },
                { "audio/mp3", new string[] { ".mp3" } },
                { "audio/x-ms-wma", new string[] { ".wma" } },
                { "audio/mp4", new string[] { ".m4a" } },
                { "audio/wav", new string[] { ".wav", "wave" } },
                { "audio/vnd.wave", new string[] { ".wav", "wave" } },
                { "audio/wave", new string[] { ".wav", "wave" } },
                { "audio/x-wav", new string[] { ".wav", "wave" } },
                { "video/mpeg", new string[] { ".mpeg", ".mpg" } },
                { "video/mp4", new string[] { ".mp4" } },
                { "video/3gpp", new string[] { ".3gpp", ".3gp" } },
                { "video/quicktime", new string[] { ".mov" } },
                { "video/x-msvideo", new string[] { ".avi" } },
                { "video/x-ms-wmv", new string[] { ".wmv" } },
                { "video/x-flv", new string[] { ".flv" } },
                { "application/vnd.oasis.opendocument.text", new string[] { ".odt" } },
                { "application/vnd.oasis.opendocument.text-template", new string[] { ".ott" } },
                { "application/vnd.oasis.opendocument.text-web", new string[] { ".oth" } },
                { "application/vnd.oasis.opendocument.text-master", new string[] { ".odm" } },
                { "application/vnd.oasis.opendocument.graphics", new string[] { ".odg" } },
                { "application/vnd.oasis.opendocument.graphics-template", new string[] { ".otg" } },
                { "application/vnd.oasis.opendocument.presentation", new string[] { ".odp" } },
                { "application/vnd.oasis.opendocument.presentation-template", new string[] { ".otp" } },
                { "application/vnd.oasis.opendocument.spreadsheet", new string[] { ".ods" } },
                { "application/vnd.oasis.opendocument.spreadsheet-template", new string[] { ".ots" } },
                { "application/vnd.oasis.opendocument.chart", new string[] { ".odc" } },
                { "application/vnd.oasis.opendocument.formula", new string[] { ".odf" } },
                { "application/vnd.oasis.opendocument.database", new string[] { ".odb" } },
                { "application/vnd.oasis.opendocument.image", new string[] { ".odi" } },
                { "application/vnd.openofficeorg.extension", new string[] { ".oxt" } },
                { "application/zip", new string[] { ".zip" } },
                { "application/x-zip", new string[] { ".zip" } },
                { "application/rar", new string[] { ".rar" } },
                { "application/x-rar-compressed", new string[] { ".rar" } },
                { "application/java-archive", new string[] { ".jar" }  },
                { "application/epub+zip", new string[] { ".epub" }  },
                { "text/x-c",  new string[] { ".c" }},
                //{ "", new string[] { "" } },
            };

        }

        //http://en.wikipedia.org/wiki/Internet_media_type
        public static string GetContentTypeFromExtension(string extension)
        {
            extension = (extension ?? "").ToLower().Trim();
            if (!extension.StartsWith(".") && extension.Length > 0)
                extension = "." + extension;
            var selectedExt = _extensionsTable.FirstOrDefault(pair => pair.Value.Contains(extension));
            return selectedExt.Key ?? "";
        }

        public static string[] GetExtensionsFromContentType(string contentType)
        {
            var mimeType = Parse(contentType);
            contentType = mimeType.ToString();
            string[] extensions;
            if (mimeType.SubType == "*")
            {
                extensions = _extensionsTable.Where(pair => new MimeType(pair.Key).Type == mimeType.Type).SelectMany(pair => pair.Value).ToArray();
            }
            else if (!_extensionsTable.TryGetValue(contentType, out extensions))
            {
                extensions = new string[0];
            }
            return extensions;
        }

        public static string GetCommonMimeType(IEnumerable<string> contentTypes)
        {
            var types = contentTypes.Select(str => MimeType.Parse(str).Type).Distinct().ToArray();
            var subTypes = contentTypes.Select(str => MimeType.Parse(str).SubType).Distinct().ToArray();
            var contentType = (types.Count() == 1 ? types.First() : "*") + "/" + (subTypes.Count() == 1 ? subTypes.First() : "*");
            return contentType;
        }

        #endregion

        /// <summary>
        /// Determines whether the mimeType1 contains the mimeType2.
        /// </summary>
        /// <param name="mimeType1">The mime type 1.</param>
        /// <param name="mimeType2">The mime type 2.</param>
        /// <returns></returns>
        public static bool Contains(string mimeType1, string mimeType2)
        {
            var mt1 = new MimeType(mimeType1);
            var mt2 = new MimeType(mimeType2);
            return (mt1.Type == "*" || mt1.Type == mt2.Type) &&
                (mt1.SubType == "*" || mt1.SubType == mt2.SubType);
        }
    }
}
