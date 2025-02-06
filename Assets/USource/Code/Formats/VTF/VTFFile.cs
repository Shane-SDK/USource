using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace USource.Formats.VTF
{
    public class VTFFile
    {
        private const string VTFHeader = "VTF";
        public VTFHeader Header { get; set; }
        public VTFResource[] Resources { get; set; }
        public VTFImage LowResImage { get; set; }
        public RawTextureData[,] Images { get; set; }
        public TextureFormat format;
        public VTFImageFormat HighResImageFormat;

        //http://wiki.xentax.com/index.php/Source_VTF
        /// <summary>
        /// Parser VTF format
        /// <br>Supported versions: 7.1 - 7.5 (maybe 7.0)</br>
        /// </summary>
        /// <param name="stream">Stream of input file</param>
        /// <param name="FileName">Name of input file (optional)</param>
        public VTFFile(Stream stream)
        {
            using (UReader FileStream = new UReader(stream))
            {
                string TempHeader = FileStream.ReadFixedLengthString(Encoding.ASCII, 4);
                if (TempHeader != VTFHeader)
                    throw new Exception("Invalid VTF header. Expected '" + VTFHeader + "', got '" + TempHeader + "'.");

                Header = new VTFHeader();

                uint VersionMajor = FileStream.ReadUInt32();
                uint VersionMinor = FileStream.ReadUInt32();
                decimal Version = VersionMajor + VersionMinor / 10m; // e.g. 7.3
                Header.Version = Version;

                uint headerSize = FileStream.ReadUInt32();
                ushort Width = FileStream.ReadUInt16();
                ushort Height = FileStream.ReadUInt16();

                Header.Flags = (VTFImageFlag)FileStream.ReadUInt32();

                ushort NumFrames = FileStream.ReadUInt16();
                ushort FirstFrame = FileStream.ReadUInt16();

                FileStream.ReadBytes(4); // padding

                Header.Reflectivity = FileStream.ReadVector3();

                FileStream.ReadBytes(4); // padding

                Header.BumpmapScale = FileStream.ReadSingle();

                HighResImageFormat = (VTFImageFormat)FileStream.ReadUInt32();
                byte MipmapCount = FileStream.ReadByte();
                VTFImageFormat LowResImageFormat = (VTFImageFormat)FileStream.ReadUInt32();
                byte LowResWidth = FileStream.ReadByte();
                byte LowResHeight = FileStream.ReadByte();

                ushort Depth = 1;
                uint NumResources = 0;

                if (Version >= 7.2m)
                {
                    Depth = FileStream.ReadUInt16();
                }
                if (Version >= 7.3m)
                {
                    FileStream.ReadBytes(3);
                    NumResources = FileStream.ReadUInt32();
                    FileStream.ReadBytes(8);
                }

                int NumFaces = 1;
                if (Header.Flags.HasFlag(VTFImageFlag.TEXTUREFLAGS_ENVMAP))
                {
                    NumFaces = Version < 7.5m && FirstFrame != 0xFFFF ? 7 : 6;
                }

                VTFImageFormatInfo HighResFormatInfo = VTFImageFormatInfo.FromFormat(HighResImageFormat);
                VTFImageFormatInfo LowResFormatInfo = VTFImageFormatInfo.FromFormat(LowResImageFormat);

                int ThumbnailSize = LowResImageFormat == VTFImageFormat.IMAGE_FORMAT_NONE ? 0 : LowResFormatInfo.GetSize(LowResWidth, LowResHeight);

                uint ThumbnailOffset = headerSize;
                long DataOffset = headerSize + ThumbnailSize;

                Resources = new VTFResource[NumResources];
                for (int i = 0; i < NumResources; i++)
                {
                    VTFResourceType type = (VTFResourceType)FileStream.ReadUInt32();
                    uint DataSize = FileStream.ReadUInt32();
                    switch (type)
                    {
                        case VTFResourceType.LowResImage:
                            // Low res image
                            ThumbnailOffset = DataSize;
                            break;
                        case VTFResourceType.Image:
                            // Regular image
                            DataOffset = DataSize;
                            break;
                        case VTFResourceType.Sheet:
                        case VTFResourceType.CRC:
                        case VTFResourceType.TextureLodSettings:
                        case VTFResourceType.TextureSettingsEx:
                        case VTFResourceType.KeyValueData:
                            // todo
                            Resources[i] = new VTFResource
                            {
                                Type = type,
                                Data = DataSize
                            };
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(type), (uint)type, "Unknown resource type");
                    }
                }

                if (LowResImageFormat != VTFImageFormat.IMAGE_FORMAT_NONE)
                {
                    FileStream.BaseStream.Position = ThumbnailOffset;
                    int thumbSize = LowResFormatInfo.GetSize(LowResWidth, LowResHeight);
                    LowResImage = new VTFImage
                    {
                        Format = LowResImageFormat,
                        Width = LowResWidth,
                        Height = LowResHeight,
                        Data = FileStream.ReadBytes(thumbSize)
                    };
                }

                // Read byte data
                FileStream.BaseStream.Position = DataOffset;
                Images = new RawTextureData[NumFrames, MipmapCount];

                format = TextureFormat.BGRA32;

                for (int MipLevel = MipmapCount - 1; MipLevel >= 0; MipLevel--)  // Progress stream reader to highest resolution texture (skip mipmaps)
                {
                    int Wid = GetMipSize(Width, MipLevel);
                    int Hei = GetMipSize(Height, MipLevel);
                    for (int FrameID = 0; FrameID < NumFrames; FrameID++)  // animated textures
                    {
                        for (int FaceID = 0; FaceID < NumFaces; FaceID++)  // Cube maps ?
                        {
                            for (int SliceID = 0; SliceID < Depth; SliceID++)  // volumetrics ???????
                            {
                                RawTextureData data = new RawTextureData { height = Hei, width = Wid };
                                Images[FrameID, MipLevel] = data;

                                // Convert everything to standard BGRA32 because I cannot flip the y axis otherwise
                                data.bytes = VTFImageFormatInfo.FromFormat(HighResImageFormat).ConvertToBgra32(FileStream.ReadBytes(HighResFormatInfo.GetSize(Wid, Hei)), Wid, Hei);
                            }
                        }
                    }
                }
            }
        }
        public static int GetMipSize(int input, int level)
        {
            int res = input >> level;
            if (res < 1) res = 1;
            return res;
        }
        public static void FlipYAxisBGRA32(byte[] bytes, int width, int height)
        {
            // Manipulate bytes to flip the image on the y axis
            Color32[] buffer = new Color32[height];
            for (int x = 0; x < width; x++)
            {
                // First pass, copy to buffer
                for (int y = 0; y < height; y++)
                {
                    // BGRA
                    int byteOffset = (x + y * width) * 4;
                    buffer[y] = new Color32(
                        bytes[byteOffset + 2],
                        bytes[byteOffset + 1],
                        bytes[byteOffset + 0],
                        bytes[byteOffset + 3]
                        );
                }

                // Second pass
                for (int y = 0; y < height; y++)
                {
                    // BGRA
                    int byteOffset = (x + y * width) * 4;
                    Color32 color = buffer[height - 1 - y];

                    bytes[byteOffset] = color.b;
                    bytes[byteOffset + 1] = color.g;
                    bytes[byteOffset + 2] = color.r;
                    bytes[byteOffset + 3] = color.a;
                }
            }
        }
    }

    public class RawTextureData
    {
        public int width;
        public int height;
        public byte[] bytes;
    }
}