using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Runtime.InteropServices;

namespace USource.Providers
{
    // https://github.com/Facepunch/gmad/blob/master/include/AddonReader.h
    public class GMAProvider : IResourceProvider
    {
        Dictionary<string, int> nameMapping;
        List<Entry> entries;
        UReader reader;
        string name;
        string json;
        string author;
        ulong steamId;
        ulong timestamp;
        long fileBlock;
        int addonVersion;
        ushort fmtVersion;
        public GMAProvider(string gmaPath)
        {
            string extension = System.IO.Path.GetExtension(gmaPath).ToLower();
            Stream gmaStream = File.OpenRead(gmaPath);
            if (extension == ".7z")  // Decompress using LZMA
            {
                Stream output = new MemoryStream();
            }

            Parse(gmaStream);
        }
        void Parse(Stream stream)
        {
            reader = new(stream);

            string gmadString = new string(reader.ReadChars(4));
            if (gmadString != "GMAD") return;
            fmtVersion = reader.ReadByte();
            steamId = reader.ReadUInt64();
            timestamp = reader.ReadUInt64();

            if (fmtVersion > 1)
            {
                string strContent = reader.ReadNullTerminatedString();
                while (!string.IsNullOrEmpty(strContent))
                    strContent = reader.ReadNullTerminatedString();
            }


            name = reader.ReadNullTerminatedString();
            json = reader.ReadNullTerminatedString();
            author = reader.ReadNullTerminatedString();
            addonVersion = reader.ReadInt32();

            entries = new();
            nameMapping = new();

            int fileNumber = 0;
            long offset = 0;

            while (reader.ReadUInt32() != 0)
            {
                string name = reader.ReadNullTerminatedString().ToLower();
                long size = reader.ReadInt64();
                uint crc = reader.ReadUInt32();

                Entry entry = new Entry
                {
                    name = name,
                    size = size,
                    crc = crc,
                    offset = offset,
                    fileNumber = fileNumber,
                };

                nameMapping[name] = fileNumber;
                entries.Add(entry);

                offset += entry.size;
                fileNumber++;
            }

            fileBlock = reader.BaseStream.Position;
        }
        public bool ContainsFile(string FilePath)
        {
            return nameMapping.ContainsKey(FilePath.ToLower());
        }
        public IEnumerable<string> GetFiles()
        {
            return entries.Select(e => e.name);
        }
        public string GetName()
        {
            return null;
        }

        public Stream OpenFile(string FilePath)
        {
            if (nameMapping.TryGetValue(FilePath.ToLower(), out int index))
            {
                Entry entry = entries[index];
                byte[] data = new byte[entry.size];
                reader.BaseStream.Position = fileBlock + entry.offset;
                reader.Read(data, 0, data.Length);
                return new MemoryStream(data);
            }

            return null;
        }

        public void OpenFile<T>(string filePath, T stream) where T : Stream
        {
            if (nameMapping.TryGetValue(filePath.ToLower(), out int index))
            {
                Entry entry = entries[index];
                byte[] data = new byte[entry.size];
                reader.BaseStream.Position = fileBlock + entry.offset;
                reader.Read(data, 0, data.Length);
                stream.Read(data, 0, data.Length);
            }

            return;
        }

        public struct Entry
        {
            public string name;
            public long size;
            public long offset;
            public uint crc;
            public int fileNumber;
        }
    }
}
