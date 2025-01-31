using UnityEngine;
using USource.Formats.Source.VTF;

namespace USource.Converters
{
    public class TextureConverter : IConverter
    {
        public enum ColorMode
        {
            RGB,
            Normal,
            Grayscale
        }
        [System.Serializable]
        public struct ImportOptions
        {
            public int maxSize;
            public TextureWrapMode wrapMode;
            public bool mipMaps;
        }
        UnityEngine.Texture2D texture;
        VTFFile vtf;
        public ImportOptions importOptions = new ImportOptions { 
            maxSize = 1024, 
            mipMaps = true, 
            wrapMode = TextureWrapMode.Repeat 
        };
        public TextureConverter(System.IO.Stream stream, ImportOptions importOptions)
        {
            vtf = new VTFFile(stream);
            this.importOptions = importOptions;
        }
        public UnityEngine.Object CreateAsset(ImportContext ctx)
        {
            // Create texture object
            Texture2D unityTexture;

            // Check if original VTF format had an alpha channel
            bool hasAlpha = VTFImageFormatInfo.FromFormat(vtf.HighResImageFormat).AlphaBitsPerPixel > 0;
            bool isNormal = vtf.Header.Flags.HasFlag(VTFImageFlag.TEXTUREFLAGS_NORMAL);
            TextureFormat format = (hasAlpha || isNormal) ? TextureFormat.RGBA32 : TextureFormat.RGB24;
            unityTexture = new Texture2D(vtf.Images[0, 0].width, vtf.Images[0, 0].height, format, importOptions.mipMaps, isNormal, true);
            unityTexture.wrapMode = importOptions.wrapMode;

            // Flip pixels on y axis
            for (int mip = 0; mip < (importOptions.mipMaps ? vtf.Images.GetLength(1) : 1); mip++)
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

            return unityTexture;
        }
    }
}
