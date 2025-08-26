using Open.FileSystemAsync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Open.FileSystemAsync
{
    public static class Path
    {
        public static char[] DIRECTORY_SEPARATORS = new char[] { '/', '\\', '|' };
        private static char[] INVALID_PATH_CHARS = System.IO.Path.GetInvalidFileNameChars();
        public static char DirectorySeparatorChar { get { return '\\'; } }

        public static IEnumerable<string> DecomposePath(string path)
        {
            path = NormalizePath(path);
            if (string.IsNullOrWhiteSpace(path))
            {
                yield break;
            }
            int nextSeparatorIndex = -1, lastSeparatorIndex = 0;
            do
            {
                nextSeparatorIndex = path.IndexOf(DirectorySeparatorChar, lastSeparatorIndex);
                if (nextSeparatorIndex >= 0)
                {
                    yield return NormalizePath(path.Substring(0, nextSeparatorIndex));
                    lastSeparatorIndex = nextSeparatorIndex + 1;
                }
                else
                {
                    yield return NormalizePath(path);
                }
            }
            while (nextSeparatorIndex >= 0);
        }

        public static string NormalizePath(string path)
        {
            var newPath = new StringBuilder(path ?? "");
            //newPath.Trim();
            newPath.Trim(DIRECTORY_SEPARATORS);
            newPath.ReplaceMany(DIRECTORY_SEPARATORS, DirectorySeparatorChar);
            return newPath.ToString();
        }

        public static IEnumerable<string> SplitPath(string path)
        {
            path = NormalizePath(path);
            var splittedPath = path.Split(DIRECTORY_SEPARATORS, StringSplitOptions.RemoveEmptyEntries);
            if (splittedPath.Length == 0)
                return new string[1] { "" };
            else
                return splittedPath;
        }

        public static string GetParentPath(string path)
        {
            path = NormalizePath(path);
            var parts = Path.SplitPath(path);
            return string.Join(DirectorySeparatorChar.ToString(), parts.Take(parts.Count() - 1).ToArray());
        }

        public static string RemoveBaseDirectory(string path)
        {
            path = NormalizePath(path);
            var parts = path.Split(DIRECTORY_SEPARATORS);
            return parts.Count() == 0 ? "" : string.Join(DirectorySeparatorChar.ToString(), parts.Skip(1).ToArray());
        }

        public static string GetFileName(string fullpath)
        {
            fullpath = NormalizePath(fullpath);
            return fullpath.Split(DIRECTORY_SEPARATORS).Last();
            //return System.IO.Path.GetFileName(fullpath);
        }

        public static string GetFileNameWithoutExtension(string fullpath)
        {
            var fileNameWithExtension = GetFileName(fullpath);
            var dotIndex = fileNameWithExtension.LastIndexOf(".");
            if (dotIndex >= 0)
                return fileNameWithExtension.Substring(0, dotIndex);
            else
                return fileNameWithExtension;
        }

        public static string GetExtension(string fullpath)
        {
            var fileNameWithExtension = GetFileName(fullpath);
            var dotIndex = fileNameWithExtension.LastIndexOf(".");
            if (dotIndex >= 0)
                return fileNameWithExtension.Substring(dotIndex, fileNameWithExtension.Length - dotIndex);
            else
                return "";
        }

        public static bool HasExtension(string path)
        {
            try
            {
                var fileNameWithExtension = GetFileName(path);
                return fileNameWithExtension.Contains(".");
            }
            catch
            {
                return false;
            }
        }

        public static bool IsSubDirectory(string baseDirectory, string subDirectory)
        {
            var parts = Path.SplitPath(subDirectory);
            if (parts.Count() > 0)
                return Combine(parts.Take(parts.Count() - 1).ToArray()) == NormalizePath(baseDirectory);
            else
                return false;
        }

        public static bool IsParentDirectory(string parentPath, string baseDirectory)
        {
            var baseParts = Path.SplitPath(baseDirectory).ToList();
            var parentParts = Path.SplitPath(parentPath).ToList();

            if (parentParts.Count > baseParts.Count)
            {
                return false;
            }

            bool isParent = true;
            for (int i = 0; i < parentParts.Count(); i++)
            {
                if (baseParts[i] != parentParts[i])
                {
                    isParent = false;
                    break;
                }
            }
            return isParent;
        }

        public static string Combine(params string[] paths)
        {
            var parts = paths.SelectMany(p => (p ?? "").Split(DIRECTORY_SEPARATORS, StringSplitOptions.RemoveEmptyEntries));
            return string.Join(DirectorySeparatorChar.ToString(), parts);
        }

        public static string GetRandomFileName()
        {
            return System.IO.Path.GetRandomFileName();
        }

        public static string GetValidPathSegment(string path)
        {
            var newPath = new StringBuilder(path ?? "");
            newPath.ReplaceMany(INVALID_PATH_CHARS, '_');
            int i = newPath.Length - 1;
            //Removes spaces and dots from the final because they are not accepted by windows
            while (i >= 0 && (newPath[i] == ' ' || newPath[i] == '.'))
            {
                newPath[i] = '_';
                i--;
            }
            return newPath.ToString();
        }

        public static string GetValidPath(string path)
        {
            return Combine(SplitPath(path).Select(segment => GetValidPathSegment(segment)).ToArray());
        }

        public static char[] GetInvalidPathChars()
        {
            return INVALID_PATH_CHARS;
        }

        public static string GetRelativePath(string basePath, string path)
        {
            basePath = NormalizePath(basePath);
            path = NormalizePath(path);
            if (path.StartsWith(basePath))
            {
                return NormalizePath(path.Substring(basePath.Length));
            }
            return path;
        }

        public static string GetUniqueFileName(string name, IEnumerable<string> usedNames)
        {
            if (usedNames != null && usedNames.Contains(name))
            {
                var nameWithoutExtension = GetFileNameWithoutExtension(name);
                var extension = GetExtension(name);
                var copyPattern = "{0} ({1}){2}";
                int i = 1;
                do
                {
                    var copy = string.Format(copyPattern, nameWithoutExtension, i, extension);
                    if (!usedNames.Contains(copy))
                        return copy;
                    i++;
                }
                while (true);
            }
            return name;
        }

        public static string GetUniqueDirectoryName(string name, IEnumerable<string> usedNames)
        {
            if (usedNames != null && usedNames.Contains(name))
            {
                var copyPattern = "{0} ({1})";
                int i = 1;
                do
                {
                    var copy = string.Format(copyPattern, name, i);
                    if (!usedNames.Contains(copy))
                        return copy;
                    i++;
                }
                while (true);
            }
            return name;
        }
    }
}
