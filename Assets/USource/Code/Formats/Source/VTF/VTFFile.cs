using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace USource.Formats.Source.VTF
{
    public class VTFFile
    {
        private const String VTFHeader = "VTF";
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
                String TempHeader = FileStream.ReadFixedLengthString(Encoding.ASCII, 4);
                if (TempHeader != VTFHeader) 
                    throw new Exception("Invalid VTF header. Expected '" + VTFHeader + "', got '" + TempHeader + "'.");

                Header = new VTFHeader();

                UInt32 VersionMajor = FileStream.ReadUInt32();
                UInt32 VersionMinor = FileStream.ReadUInt32();
                Decimal Version = VersionMajor + (VersionMinor / 10m); // e.g. 7.3
                Header.Version = Version;

                UInt32 headerSize = FileStream.ReadUInt32();
                UInt16 Width = FileStream.ReadUInt16();
                UInt16 Height = FileStream.ReadUInt16();

                Header.Flags = (VTFImageFlag) FileStream.ReadUInt32();

                UInt16 NumFrames = FileStream.ReadUInt16();
                UInt16 FirstFrame = FileStream.ReadUInt16();

                FileStream.ReadBytes(4); // padding

                Header.Reflectivity = FileStream.ReadVector3();

                FileStream.ReadBytes(4); // padding

                Header.BumpmapScale = FileStream.ReadSingle();

                HighResImageFormat = (VTFImageFormat) FileStream.ReadUInt32();
                Byte MipmapCount = FileStream.ReadByte();
                VTFImageFormat LowResImageFormat = (VTFImageFormat) FileStream.ReadUInt32();
                Byte LowResWidth = FileStream.ReadByte();
                Byte LowResHeight = FileStream.ReadByte();

                UInt16 Depth = 1;
                UInt32 NumResources = 0;

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

                Int32 NumFaces = 1;
                if (Header.Flags.HasFlag(VTFImageFlag.TEXTUREFLAGS_ENVMAP))
                {
                    NumFaces = Version < 7.5m && FirstFrame != 0xFFFF ? 7 : 6;
                }

                VTFImageFormatInfo HighResFormatInfo = VTFImageFormatInfo.FromFormat(HighResImageFormat);
                VTFImageFormatInfo LowResFormatInfo = VTFImageFormatInfo.FromFormat(LowResImageFormat);

                Int32 ThumbnailSize = LowResImageFormat == VTFImageFormat.IMAGE_FORMAT_NONE ? 0 : LowResFormatInfo.GetSize(LowResWidth, LowResHeight);

                UInt32 ThumbnailOffset = headerSize;
                Int64 DataOffset = headerSize + ThumbnailSize;

                Resources = new VTFResource[NumResources];
                for (Int32 i = 0; i < NumResources; i++)
                {
                    VTFResourceType type = (VTFResourceType) FileStream.ReadUInt32();
                    UInt32 DataSize = FileStream.ReadUInt32();
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
                            throw new ArgumentOutOfRangeException(nameof(type), (uint) type, "Unknown resource type");
                    }
                }

                if (LowResImageFormat != VTFImageFormat.IMAGE_FORMAT_NONE)
                {
                    FileStream.BaseStream.Position = ThumbnailOffset;
                    Int32 thumbSize = LowResFormatInfo.GetSize(LowResWidth, LowResHeight);
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

                for (Int32 MipLevel = MipmapCount - 1; MipLevel >= 0; MipLevel--)  // Progress stream reader to highest resolution texture (skip mipmaps)
                {
                    Int32 Wid = GetMipSize(Width, MipLevel);
                    Int32 Hei = GetMipSize(Height, MipLevel);
                    for (Int32 FrameID = 0; FrameID < NumFrames; FrameID++)  // animated textures
                    {
                        for (Int32 FaceID = 0; FaceID < NumFaces; FaceID++)  // Cube maps ?
                        {
                            for (Int32 SliceID = 0; SliceID < Depth; SliceID++)  // volumetrics ???????
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
        public static Int32 GetMipSize(Int32 input, Int32 level)
        {
            Int32 res = input >> level;
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
                    int byteOffset = ((x) + y * width) * 4;
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