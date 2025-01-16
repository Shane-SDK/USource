using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace USource
{
    /// <summary>
    /// Provides Source assets through the OS' file explorer
    /// </summary>
    public class DirProvider : IResourceProvider
    {
        public static readonly string[] subFolders = new string[]
        {
            "models",
            "materials",
            "maps",
            "sound"
        };
        readonly string root;
        public DirProvider(string directory)
        {
            if (!string.IsNullOrEmpty(directory))
                root = directory;
        }

        public bool ContainsFile(string assetPath)
        {
            string path = Path.Combine(root, assetPath);
            if (File.Exists(path))
            {
                return true;
            }

            return false;
        }
        public void OpenFile<T>(string assetPath, T stream) where T : Stream
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(root, assetPath));
            stream.Write(bytes);
        }
        public Stream OpenFile(string assetPath)
        {
            if (ContainsFile(assetPath))
            {
                return File.OpenRead(Path.Combine(root, assetPath));
            }

            throw new FileLoadException(assetPath + " NOT FOUND!");
        }
        public int GetPriority() => 10;
        public string GetName() => root.Split('/')[^1] + " (folder)";

        public IEnumerable<string> GetFiles()
        {
            //UnityEngine.Debug.Log($"ENUMERATING: {root}");

            foreach (string subFolder in subFolders)
            {
                string newPath = $"{root}/{subFolder}";
                if (Directory.Exists(newPath))
                {
                    foreach (string path in System.IO.Directory.EnumerateFiles(newPath, "*", System.IO.SearchOption.AllDirectories))
                    {
                        yield return path.Substring(root.Length + 1, path.Length - (root.Length + 1));
                        continue;
                    }
                }
            }
        }

        //public void CloseStreams()
        //{
        //    if (currentFile != null)
        //    {
        //        currentFile.Dispose();
        //        currentFile.Close();
        //        return;
        //    }

        //    return;
        //}
    }
}