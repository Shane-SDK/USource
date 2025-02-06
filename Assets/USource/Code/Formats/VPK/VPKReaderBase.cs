using System;
using System.IO;

namespace USource.Formats.VPK
{
    internal class VPKReaderBase : UReader
    {
        public VPKReaderBase(Stream InputStream)
            : base(InputStream)
        {
            this.InputStream = InputStream;

            if (!InputStream.CanRead)
                throw new FileLoadException("Can't read unreadable archive!");
        }

        public void ReadDirectories(VPKFile RootArchive)
        {
            while (true)
            {
                string Extension = ReadNullTerminatedString();
                if (string.IsNullOrEmpty(Extension))
                    break;

                while (true)
                {
                    string Path = ReadNullTerminatedString();
                    if (string.IsNullOrEmpty(Path))
                        break;

                    ReadEntries(RootArchive, Extension, Path);
                }
            }
        }

        public void ReadEntries(VPKFile RootArchive, string Extension, string Path)
        {
            while (true)
            {
                string FileName = ReadNullTerminatedString();
                if (string.IsNullOrEmpty(FileName))
                    break;

                uint CRC = ReadUInt32();
                ushort PreloadBytes = ReadUInt16();
                ushort ArchiveIndex = ReadUInt16();
                uint EntryOffset = ReadUInt32();
                uint EntryLength = ReadUInt32();
                // skip terminator
                ReadUInt16();
                uint preloadDataOffset = (uint)BaseStream.Position;
                if (PreloadBytes > 0)
                {
                    BaseStream.Position += PreloadBytes;
                }

                ArchiveIndex = ArchiveIndex == 32767 ? (ushort)0 : ArchiveIndex;

                Path = Path.ToLower();
                FileName = FileName.ToLower();
                Extension = Extension.ToLower();

                RootArchive.Entries.Add(string.Format("{0}/{1}.{2}", Path, FileName, Extension), new VPKEntry(RootArchive, CRC, PreloadBytes, preloadDataOffset, ArchiveIndex, EntryOffset, EntryLength));
            }
        }
    }
}

