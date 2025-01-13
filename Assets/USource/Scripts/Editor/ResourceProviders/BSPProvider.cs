using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
namespace USource
{
    /// <summary>
    /// Opens the pak lump from a compiled map
    /// </summary>
    public class BSPProvider : IResourceProvider
    {
        readonly PAKProvider pakProvider;
        public BSPProvider(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            reader.BaseStream.Position = 0;
            reader.ReadBytes(4);  // Skip identifier

            int version = reader.ReadInt32();
            int lumpStructSize = 16;
            int pakLumpIndex = 40;

            // Read the 40th lump struct (pak)
            reader.BaseStream.Position = reader.BaseStream.Position + pakLumpIndex * lumpStructSize;
            int offset = reader.ReadInt32();
            int length = reader.ReadInt32();
            reader.BaseStream.Position = offset;
            Stream pakStream = new MemoryStream(reader.ReadBytes(length));
            pakProvider = new PAKProvider(pakStream, "BSP");
        }
        public bool ContainsFile(string FilePath)
        {
            return pakProvider.ContainsFile(FilePath);
        }

        public IEnumerable<string> GetFiles()
        {
            return pakProvider.GetFiles();
        }

        public string GetName()
        {
            return pakProvider.GetName() + " (bsp)";
        }

        public Stream OpenFile(string FilePath)
        {
            return pakProvider.OpenFile(FilePath);
        }

        public void OpenFile<T>(string filePath, T stream) where T : Stream
        {
            pakProvider.OpenFile(filePath, stream);
        }
    }
}
