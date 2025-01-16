using System;
using System.IO;
using System.Collections.Generic;

namespace USource
{
    /// <summary>
    /// Provides a way of accessing Source engine assets
    /// </summary>
    public interface IResourceProvider
    {
        public Stream this[Location location]
        {
            get
            {
                return OpenFile(location.SourcePath);
            }
        }
        bool ContainsFile(string FilePath);
        Stream OpenFile(string FilePath);
        /// <summary>
        /// Write file to stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="stream"></param>
        void OpenFile<T>(string filePath, T stream) where T : Stream;
        public string GetName();
        public virtual int GetPriority() => 0;
        public IEnumerable<string> GetFiles();
        public bool TryGetFile(string filePath, out Stream stream)
        {
            if (ContainsFile(filePath))
            {
                stream = OpenFile(filePath);
                return true;
            }

            stream = null;
            return false;
        }
    }
}