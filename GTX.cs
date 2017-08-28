using System;
using System.Threading.Tasks;

namespace swf2lm
{
    public class GTX
    {

        public enum TextureFormat
        {
            DXT1 = 0,
            DXT3 = 1,
            DXT5 = 2,
            RGBA = 14,
            ARGB = 17,
            BC4 = 21,
            BC5 = 22
        }

        public static int getBPP(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.DXT3:
                case TextureFormat.DXT5:
                case TextureFormat.BC5:
                    return 0x80;
                case TextureFormat.DXT1:
                case TextureFormat.BC4:
                    return 0x40;
                case TextureFormat.RGBA:
                case TextureFormat.ARGB:
                    return 0x20;
                default:
                    throw new NotImplementedException();
            }
        }

        public static byte[] swizzle(byte[] data, int width, int height, TextureFormat format, int pitch, int swizzleIn)
        {
            byte[] swizzledData = new byte[data.Length];

            int swizzle = (swizzleIn & 0x700) >> 8;
            int bpp = getBPP(format);
            int blockSize = bpp / 8;

            if (format != TextureFormat.RGBA && format != TextureFormat.ARGB)
            {
                width /= 4;
                height /= 4;
            }

            Parallel.For(0, width * height, i =>
            {
                int pos = surfaceAddrFromCoordMacroTiled(i % width, i / width, bpp, pitch, swizzle);
                Array.Copy(data, pos, swizzledData, i * blockSize, blockSize);
            });

            return swizzledData;
        }

        public static int surfaceAddrFromCoordMacroTiled(int x, int y, int bpp, int pitch, int swizzle)
        {
            int pipe = ((y ^ x) >> 3) & 1;
            int bank = (((y / 32) ^ (x >> 3)) & 1) | ((y ^ x) & 16) >> 3;
            int bankPipe = ((pipe + bank * 2) ^ swizzle) % 9;
            int macroTileBytes = (bpp * 512 + 7) >> 3;
            int macroTileOffset = (x / 32 + pitch / 32 * (y / 16)) * macroTileBytes;
            int unk = (bpp * getPixelIndex(x, y, bpp) + macroTileOffset) >> 3;

            return (bankPipe << 8) | ((bankPipe % 2) << 8) | ((unk & ~0xFF) << 3) | (unk & 0xFF);
        }

        static int getPixelIndex(int x, int y, int bpp)
        {
            if (bpp == 0x80)
                return ((x & 7) << 1) | ((y & 6) << 3) | (y & 1);
            else if (bpp == 0x40)
                return ((x & 6) << 1) | (x & 1) | ((y & 6) << 3) | ((y & 1) << 1);
            else if (bpp == 0x20)
                return ((x & 4) << 1) | (x & 3) | ((y & 6) << 3) | ((y & 1) << 2);

            return 0;
        }
    }
}
