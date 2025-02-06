using System;

namespace USource.Formats.VTF
{
    public static class DXTDecompress
    {
        public static void DecompressDXT1(byte[] Buffer, byte[] Data, int Width, int Height)
        {
            int Position = 0;
            byte[] c = new byte[16];
            for (int y = 0; y < Height; y += 4)
            {
                for (int x = 0; x < Width; x += 4)
                {
                    int c0 = Data[Position++];
                    c0 |= Data[Position++] << 8;

                    int c1 = Data[Position++];
                    c1 |= Data[Position++] << 8;

                    c[0] = (byte)((c0 & 0xF800) >> 8);
                    c[1] = (byte)((c0 & 0x07E0) >> 3);
                    c[2] = (byte)((c0 & 0x001F) << 3);
                    c[3] = 255;

                    c[4] = (byte)((c1 & 0xF800) >> 8);
                    c[5] = (byte)((c1 & 0x07E0) >> 3);
                    c[6] = (byte)((c1 & 0x001F) << 3);
                    c[7] = 255;

                    if (c0 > c1)
                    {
                        // No Alpha channel

                        c[8] = (byte)((2 * c[0] + c[4]) / 3);
                        c[9] = (byte)((2 * c[1] + c[5]) / 3);
                        c[10] = (byte)((2 * c[2] + c[6]) / 3);
                        c[11] = 255;

                        c[12] = (byte)((c[0] + 2 * c[4]) / 3);
                        c[13] = (byte)((c[1] + 2 * c[5]) / 3);
                        c[14] = (byte)((c[2] + 2 * c[6]) / 3);
                        c[15] = 255;
                    }
                    else
                    {
                        // 1-bit Alpha channel

                        c[8] = (byte)((c[0] + c[4]) / 2);
                        c[9] = (byte)((c[1] + c[5]) / 2);
                        c[10] = (byte)((c[2] + c[6]) / 2);
                        c[11] = 255;
                        c[12] = 0;
                        c[13] = 0;
                        c[14] = 0;
                        c[15] = 0;
                    }

                    int Bytes = Data[Position++];
                    Bytes |= Data[Position++] << 8;
                    Bytes |= Data[Position++] << 16;
                    Bytes |= Data[Position++] << 24;

                    for (int yy = 0; yy < 4; yy++)
                    {
                        for (int xx = 0; xx < 4; xx++)
                        {
                            int xPosition = x + xx;
                            int yPosition = y + yy;
                            if (xPosition < Width && yPosition < Height)
                            {
                                int Index = Bytes & 0x0003;
                                Index *= 4;
                                int Pointer = yPosition * Width * 4 + xPosition * 4;
                                Buffer[Pointer + 0] = c[Index + 2]; // b
                                Buffer[Pointer + 1] = c[Index + 1]; // g
                                Buffer[Pointer + 2] = c[Index + 0]; // r
                                Buffer[Pointer + 3] = c[Index + 3]; // a
                            }
                            Bytes >>= 2;
                        }
                    }
                }
            }
        }

        public static void DecompressDXT3(byte[] Buffer, byte[] Data, int Width, int Height)
        {
            int Position = 0;
            byte[] c = new byte[16];
            byte[] a = new byte[8];
            for (int y = 0; y < Height; y += 4)
            {
                for (int x = 0; x < Width; x += 4)
                {
                    for (int i = 0; i < 8; i++)
                        a[i] = Data[Position++];

                    int c0 = Data[Position++];
                    c0 |= Data[Position++] << 8;

                    int c1 = Data[Position++];
                    c1 |= Data[Position++] << 8;

                    c[0] = (byte)((c0 & 0xF800) >> 8);
                    c[1] = (byte)((c0 & 0x07E0) >> 3);
                    c[2] = (byte)((c0 & 0x001F) << 3);
                    c[3] = 255;

                    c[4] = (byte)((c1 & 0xF800) >> 8);
                    c[5] = (byte)((c1 & 0x07E0) >> 3);
                    c[6] = (byte)((c1 & 0x001F) << 3);
                    c[7] = 255;

                    c[8] = (byte)((2 * c[0] + c[4]) / 3);
                    c[9] = (byte)((2 * c[1] + c[5]) / 3);
                    c[10] = (byte)((2 * c[2] + c[6]) / 3);
                    c[11] = 255;

                    c[12] = (byte)((c[0] + 2 * c[4]) / 3);
                    c[13] = (byte)((c[1] + 2 * c[5]) / 3);
                    c[14] = (byte)((c[2] + 2 * c[6]) / 3);
                    c[15] = 255;

                    int Bytes = Data[Position++];
                    Bytes |= Data[Position++] << 8;
                    Bytes |= Data[Position++] << 16;
                    Bytes |= Data[Position++] << 24;

                    for (int yy = 0; yy < 4; yy++)
                    {
                        for (int xx = 0; xx < 4; xx++)
                        {
                            int xPosition = x + xx;
                            int yPosition = y + yy;
                            int aIndex = yy * 4 + xx;
                            if (xPosition < Width && yPosition < Height)
                            {
                                int Index = Bytes & 0x0003;
                                Index *= 4;
                                byte Alpha = (byte)(a[aIndex >> 1] >> (aIndex << 2 & 0x07) & 0x0f);
                                Alpha = (byte)(Alpha << 4 | Alpha);
                                int Pointer = yPosition * Width * 4 + xPosition * 4;
                                Buffer[Pointer + 0] = c[Index + 2]; // b
                                Buffer[Pointer + 1] = c[Index + 1]; // g
                                Buffer[Pointer + 2] = c[Index + 0]; // r
                                Buffer[Pointer + 3] = Alpha; // a
                            }
                            Bytes >>= 2;
                        }
                    }
                }
            }
        }

        public static void DecompressDXT5(byte[] Buffer, byte[] Data, int Width, int Height)
        {
            int Position = 0;
            byte[] c = new byte[16];
            int[] a = new int[8];
            for (int y = 0; y < Height; y += 4)
            {
                for (int x = 0; x < Width; x += 4)
                {
                    byte a0 = Data[Position++];
                    byte a1 = Data[Position++];

                    a[0] = a0;
                    a[1] = a1;

                    if (a0 > a1)
                    {
                        a[2] = (6 * a[0] + 1 * a[1] + 3) / 7;
                        a[3] = (5 * a[0] + 2 * a[1] + 3) / 7;
                        a[4] = (4 * a[0] + 3 * a[1] + 3) / 7;
                        a[5] = (3 * a[0] + 4 * a[1] + 3) / 7;
                        a[6] = (2 * a[0] + 5 * a[1] + 3) / 7;
                        a[7] = (1 * a[0] + 6 * a[1] + 3) / 7;
                    }
                    else
                    {
                        a[2] = (4 * a[0] + 1 * a[1] + 2) / 5;
                        a[3] = (3 * a[0] + 2 * a[1] + 2) / 5;
                        a[4] = (2 * a[0] + 3 * a[1] + 2) / 5;
                        a[5] = (1 * a[0] + 4 * a[1] + 2) / 5;
                        a[6] = 0x00;
                        a[7] = 0xFF;
                    }

                    long aIndex = 0L;
                    for (int i = 0; i < 6; i++)
                        aIndex |= (long)Data[Position++] << 8 * i;

                    int c0 = Data[Position++];
                    c0 |= Data[Position++] << 8;

                    int c1 = Data[Position++];
                    c1 |= Data[Position++] << 8;

                    c[0] = (byte)((c0 & 0xF800) >> 8);
                    c[1] = (byte)((c0 & 0x07E0) >> 3);
                    c[2] = (byte)((c0 & 0x001F) << 3);
                    c[3] = 255;

                    c[4] = (byte)((c1 & 0xF800) >> 8);
                    c[5] = (byte)((c1 & 0x07E0) >> 3);
                    c[6] = (byte)((c1 & 0x001F) << 3);
                    c[7] = 255;

                    c[8] = (byte)((2 * c[0] + c[4]) / 3);
                    c[9] = (byte)((2 * c[1] + c[5]) / 3);
                    c[10] = (byte)((2 * c[2] + c[6]) / 3);
                    c[11] = 255;

                    c[12] = (byte)((c[0] + 2 * c[4]) / 3);
                    c[13] = (byte)((c[1] + 2 * c[5]) / 3);
                    c[14] = (byte)((c[2] + 2 * c[6]) / 3);
                    c[15] = 255;

                    int Bytes = Data[Position++];
                    Bytes |= Data[Position++] << 8;
                    Bytes |= Data[Position++] << 16;
                    Bytes |= Data[Position++] << 24;

                    for (int yy = 0; yy < 4; yy++)
                    {
                        for (int xx = 0; xx < 4; xx++)
                        {
                            int xPosition = x + xx;
                            int yPosition = y + yy;
                            if (xPosition < Width && yPosition < Height)
                            {
                                int Index = Bytes & 0x0003;
                                Index *= 4;
                                byte Alpha = (byte)a[aIndex & 0x07];
                                int Pointer = yPosition * Width * 4 + xPosition * 4;
                                Buffer[Pointer + 0] = c[Index + 2]; // b
                                Buffer[Pointer + 1] = c[Index + 1]; // g
                                Buffer[Pointer + 2] = c[Index + 0]; // r
                                Buffer[Pointer + 3] = Alpha; // a
                            }
                            Bytes >>= 2;
                            aIndex >>= 3;
                        }
                    }
                }
            }
        }
    }
}