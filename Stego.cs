using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Secure_Image
{
    static class Stego
    {
        // Capacity: 3 bits/pixel  => bytes = pixels * 3 / 8
        public static int CapacityBytes(Bitmap bmp) => (bmp.Width * bmp.Height * 3) / 8;

        public static Bitmap EmbedBytes(Bitmap src, byte[] data)
        {
            var bmp = new Bitmap(src); // copy
            int bitIdx = 0, totalBits = data.Length * 8;

            for (int y = 0; y < bmp.Height && bitIdx < totalBits; y++)
            {
                for (int x = 0; x < bmp.Width && bitIdx < totalBits; x++)
                {
                    var c = bmp.GetPixel(x, y);
                    byte r = c.R, g = c.G, b = c.B;

                    // Write one bit into each channel LSB (R,G,B)
                    if (bitIdx < totalBits) r = (byte)((r & 0xFE) | GetBit(data, bitIdx++));
                    if (bitIdx < totalBits) g = (byte)((g & 0xFE) | GetBit(data, bitIdx++));
                    if (bitIdx < totalBits) b = (byte)((b & 0xFE) | GetBit(data, bitIdx++));

                    bmp.SetPixel(x, y, Color.FromArgb(c.A, r, g, b));
                }
            }

            if (bitIdx < totalBits)
                throw new Exception("Not enough capacity in image to embed bytes.");

            return bmp;
        }

        public static byte[] ExtractBytes(Bitmap bmp, int lengthBytes)
        {
            int totalBits = lengthBytes * 8;
            var bits = new int[totalBits];
            int bitIdx = 0;

            for (int y = 0; y < bmp.Height && bitIdx < totalBits; y++)
            {
                for (int x = 0; x < bmp.Width && bitIdx < totalBits; x++)
                {
                    var c = bmp.GetPixel(x, y);
                    if (bitIdx < totalBits) bits[bitIdx++] = c.R & 1;
                    if (bitIdx < totalBits) bits[bitIdx++] = c.G & 1;
                    if (bitIdx < totalBits) bits[bitIdx++] = c.B & 1;
                }
            }

            if (bitIdx < totalBits)
                throw new Exception("Image does not contain enough embedded data.");

            return BitsToBytes(bits);
        }

        static int GetBit(byte[] data, int bitIndex)
        {
            int byteIndex = bitIndex / 8;
            int bitInByte = 7 - (bitIndex % 8);
            return (data[byteIndex] >> bitInByte) & 1;
        }

        static byte[] BitsToBytes(int[] bits)
        {
            var bytes = new byte[(bits.Length + 7) / 8];
            for (int i = 0; i < bits.Length; i++)
            {
                int bi = i / 8;
                int pos = 7 - (i % 8);
                bytes[bi] |= (byte)(bits[i] << pos);
            }
            return bytes;
        }
    }
}
