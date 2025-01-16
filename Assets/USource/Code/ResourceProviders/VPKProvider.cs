using System;
using System.Collections.Generic;
using System.IO;
using USource.Formats.Source.VPK;

namespace USource
{
    /// <summary>
    /// Provides Source assets through VPK files
    /// </summary>
    public class VPKProvider : IResourceProvider
    {
        VPKFile VPK;
        readonly string name;
        public VPKProvider(String file)
        {
            if (VPK == null)
            {
                VPK = new VPKFile(file);
            }

            name = file.Split('/')[^1];
        }

        public Boolean ContainsFile(String FilePath)
        {
            return VPK.Entries.ContainsKey(FilePath);
        }

        public Stream OpenFile(String FilePath)
        {
            if (ContainsFile(FilePath))
            {
                return VPK.Entries[FilePath].ReadAnyDataStream();
            }

            //throw new FileLoadException(FilePath + " NOT FOUND!");
            return null;
        }
        public string GetName() => name;

        public IEnumerable<string> GetFiles()
        {
            foreach (string key in VPK.Entries.Keys)
                yield return key;
        }

        public void OpenFile<T>(string filePath, T stream) where T : Stream
        {
            VPK.Entries[filePath].CopyDataStreamTo(stream);
        }

        //public void CloseStreams()
        //{
        //    //if (VPK != null)
        //    //{
        //    //    VPK.Dispose();
        //    //}

        //    //VPK = null;
        //}
    }
}