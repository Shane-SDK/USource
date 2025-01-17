using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using USource.Formats.Source.VTF;
using static USource.Formats.Source.VBSP.VBSPStruct;
using static USource.ResourceManager;

namespace USource.Converters
{
    public class Texture : Converter
    {
        public enum ImportOptions
        {
            Normal = 1 << 0,
            Grayscale = 1 << 1,
        }
        UnityEngine.Texture2D texture;
        VTFFile vtf;
        readonly ImportOptions importFlags;
        public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        public bool mipmaps = true;
        public int maxSize = 1024;
        public Texture(string sourcePath, System.IO.Stream stream, ImportOptions importFlags, int maxSize = 1024) : base(sourcePath, stream)
        {
            vtf = new VTFFile(stream);
            this.importFlags = importFlags;
            this.maxSize = maxSize;
        }
        public override UnityEngine.Object CreateAsset(ImportContext ctx)
        {
            // Create texture object
            Texture2D unityTexture;

            // Check if original VTF format had an alpha channel
            bool hasAlpha = VTFImageFormatInfo.FromFormat(vtf.HighResImageFormat).AlphaBitsPerPixel > 0;
            bool isNormal = this.importFlags.HasFlag(ImportOptions.Normal);
            TextureFormat format = (hasAlpha || isNormal) ? TextureFormat.RGBA32 : TextureFormat.RGB24;
            unityTexture = new Texture2D(vtf.Images[0, 0].width, vtf.Images[0, 0].height, format, mipmaps, isNormal, true);
            unityTexture.wrapMode = wrapMode;

            // Flip pixels on y axis
            for (int mip = 0; mip < (mipmaps ? vtf.Images.GetLength(1) : 1); mip++)
            {
                RawTextureData data = vtf.Images[0, mip];

                byte[] imageData = data.bytes;
                VTFFile.FlipYAxisBGRA32(imageData, data.width, data.height);

                // Switch channels from BGR to RGB
                if (format == TextureFormat.RGBA32)
                {
                    for (int i = 0; i < imageData.Length / 4; i++)  // For each pixel
                    {
                        int byteOffset = i * 4;
                        byte red = imageData[byteOffset + 2];
                        byte blue = imageData[byteOffset];

                        // Swap red and blue channels

                        if (isNormal)
                        {
                            imageData[byteOffset + 3] = 128;  // Red channel
                            imageData[byteOffset] = 255;  // Blue channel - Green channel is untouched
                            imageData[byteOffset + 3] = red;  // alpha
                        }
                        else
                        {
                            imageData[byteOffset] = red;
                            imageData[byteOffset + 2] = blue;
                        }

                        //imageData[byteOffset] = 128;  // blue
                        //imageData[byteOffset + 1] = color.g;  // green
                        //imageData[byteOffset + 2] = 255;  // red
                        //imageData[byteOffset + 3] = color.r;  // alpha
                    }
                }
                else // RGB24
                {
                    byte[] newByteData = new byte[data.width * data.height * 3];
                    for (int i = 0; i < imageData.Length / 4; i++)  // For each pixel
                    {
                        newByteData[i * 3 + 2] = imageData[i * 4];
                        newByteData[i * 3 + 1] = imageData[i * 4 + 1];
                        newByteData[i * 3] = imageData[i * 4 + 2];
                    }

                    imageData = newByteData;
                }
                

                unityTexture.SetPixelData<byte>(imageData, mip);
            }

            // ensure maxsize is a power of two
            //maxSize = (int)Mathf.Pow(2, Mathf.CeilToInt(Mathf.Log(maxSize, 2)));

            //if (data.width > maxSize || data.height > maxSize)
            //{
            //    RenderTexture copySourceTexture = new RenderTexture(Mathf.Min(data.width, maxSize), Mathf.Min(data.height, maxSize), 0, isNormal ? GraphicsFormat.B8G8R8A8_UNorm : GraphicsFormat.B8G8R8A8_SRGB);
            //    RenderTexture.active = copySourceTexture;
            //    unityTexture.Apply(false, false);
            //    Graphics.Blit(unityTexture, copySourceTexture);

            //    unityTexture.Reinitialize(Mathf.Min(data.width, maxSize), Mathf.Min(data.height, maxSize));
            //    unityTexture.ReadPixels(new Rect(0, 0, copySourceTexture.width, copySourceTexture.height), 0, 0);
            //}

            unityTexture.Compress(true);
            unityTexture.Apply(true, true);

            unityObject = unityTexture;

            return unityTexture;
        }
    }
}
