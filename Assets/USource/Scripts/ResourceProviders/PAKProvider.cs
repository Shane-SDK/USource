using System;
using System.IO;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;

namespace USource
{
    public class PAKProvider : IResourceProvider
    {
        public ZipFile IPAK;
        public Dictionary<String, Int32> files;
        readonly string name;

        public PAKProvider(Stream stream, string pakName)
        {
            name = pakName;
            IPAK = new ZipFile(stream);
            files = new Dictionary<String, Int32>();

            for(Int32 EntryID = 0; EntryID < IPAK.Count; EntryID++)
            {
                ZipEntry entry = IPAK[EntryID];
                if (entry.IsFile)
                {
                    String fileName = entry.Name.ToLower().Replace("\\", "/");
                    if (ContainsFile(fileName))
                        continue;

                    files.Add(fileName, EntryID);
                }
            }
        }
        public Boolean ContainsFile(String FilePath)
        {
            return files.ContainsKey(FilePath);
        }

        public Stream OpenFile(String FilePath)
        {
            if (ContainsFile(FilePath))
            {
                return IPAK.GetInputStream(files[FilePath]);
            }

            throw new FileLoadException(FilePath + " NOT FOUND!");
        }
        public int GetPriority() => -1;
        public string GetName() => name;

        public IEnumerable<string> GetFiles()
        {
            foreach (string key in files.Keys)
                yield return key;
        }

        public void OpenFile<T>(string filePath, T stream) where T : Stream
        {
            Stream fileStream = OpenFile(filePath);
            byte[] bytes = new BinaryReader(fileStream).ReadBytes((int)fileStream.Length);
            stream.Write(bytes, 0, bytes.Length);
            fileStream.Dispose();
        }

        //public void CloseStreams()
        //{
        //    files.Clear();
        //    files = null;
        //    IPAK.Close();
        //    IPAK = null;
        //}
    }
}