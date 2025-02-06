using System;
using System.IO;
using System.Collections.Generic;

namespace USource.Formats.VPK
{
    internal class ArchiveParsingException : Exception
    {
        public ArchiveParsingException()
        {
        }

        public ArchiveParsingException(string message)
            : base(message)
        {
        }

        public ArchiveParsingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public sealed class VPKFile : IDisposable
    {
        public bool Loaded { get; private set; }
        public bool IsMultiPart
        {
            get
            {
                return Parts.Count > 1;
            }
        }

        private VPKReaderBase Reader { get; set; }
        private bool Disposed { get; set; } // To detect redundant calls

        public Dictionary<string, VPKEntry> Entries = new Dictionary<string, VPKEntry>();
        internal Dictionary<int, VPKFilePart> Parts { get; } = new Dictionary<int, VPKFilePart>();
        internal VPKFilePart MainPart
        {
            get
            {
                return Parts[MainPartIndex];
            }
        }

        internal const int MainPartIndex = -1;

        /// <summary>
        /// Loads the specified vpk archive by filename, if it's a _dir.vpk file it'll load related numbered vpks automatically
        /// </summary>
        /// <param name="FileName">A vpk archive ending in _dir.vpk</param>
        public VPKFile(string FileName)
        {
            Load(new FileStream(FileName, FileMode.Open, FileAccess.Read), FileName);
        }

        /// <summary>
        /// Loads the specified vpk archive by filename, if it's a _dir.vpk file it'll load related numbered vpks automatically
        /// </summary>
        /// <param name="FileName">A vpk archive ending in _dir.vpk</param>
        public void Load(string FileName)
        {
            Load(new FileStream(FileName, FileMode.Open, FileAccess.Read), FileName);
        }

        /// <summary>
        /// The main Load function, the related parts need to be numbered correctly as "archivename_01.vpk" and so forth
        /// </summary>
        /// <param name="Stream"></param>
        /// <param name="FileName"></param>
        public void Load(Stream Stream, string FileName = "")
        {
            if (Loaded)
                throw new NotSupportedException("Tried to call Load on a VpkArchive that is already loaded, dispose and create a new one instead");

            if (string.IsNullOrEmpty(FileName))
                throw new FileLoadException("File name is empty!!!");

            Reader = new VPKReaderBase(Stream);

            uint Signature = Reader.ReadUInt32();
            uint Version = Reader.ReadUInt32();

            if (Signature != 0x55aa1234 && (Version > 2 || Version < 1))
            {
                Dispose();
                throw new ArchiveParsingException("Invalid archive header");
            }

            // skip unneeded bytes
            if (Version == 1 || Version == 2)
            {
                Reader.ReadInt32(); // - TreeSize;
                if (Version == 2)
                    Reader.ReadBytes(12);
            }

            AddMainPart(FileName, Stream);

            //TODO:
            //OPTIMIZE PARSING
            string Folder = Path.GetDirectoryName(FileName) ?? "";
            string NameWithoutExtension = Path.GetFileNameWithoutExtension(FileName) ?? "";
            //String Extension = Path.GetExtension(FileName);

            string BaseName = NameWithoutExtension.Substring(0, NameWithoutExtension.Length - 4);

            string[] MatchingFiles = Directory.GetFiles(Folder, BaseName + "_???.vpk");
            foreach (string MatchedFile in MatchingFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(MatchedFile);
                ushort Index;
                if (ushort.TryParse(fileName.Substring(fileName.Length - 3), out Index))
                {
                    AddPart(MatchedFile, new FileStream(MatchedFile, FileMode.Open, FileAccess.Read), Index);
                }
            }

            Reader.ReadDirectories(this);

            Loaded = true;
        }

        private void AddMainPart(string filename, Stream stream = null)
        {
            if (stream == null)
            {
                stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            }
            AddPart(filename, stream, MainPartIndex);
        }

        private void AddPart(string filename, Stream stream, int index)
        {
            Parts.Add(index, new VPKFilePart(index, filename, stream));
        }

        #region IDisposable Support

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    foreach (var partkv in Parts)
                    {
                        partkv.Value.PartStream?.Dispose();
                    }
                    Parts.Clear();
                    Entries.Clear();
                }
                Reader.Dispose();
                Reader.Close();
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                Disposed = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(Boolean disposing) above.
            Dispose(true);
            GC.Collect();
        }
        #endregion
    }
}