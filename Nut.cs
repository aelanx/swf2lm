using System;
using System.IO;

namespace swf2lm
{
    public class Nut
    {
        public byte[] data;
        public int id;
        public int width;
        public int height;
        public string filename;
        public GTX.TextureFormat format;

        public Nut()
        {
        }

        //public void setPixelFormatFromNutFormat(int typet)
        //{
        //    switch (typet)
        //    {
        //        case 0x0:
        //            type = PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
        //            break;
        //        case 0x1:
        //            type = PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
        //            break;
        //        case 0x2:
        //            type = PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
        //            break;
        //        case 14:
        //            type = PixelInternalFormat.Rgba;
        //            utype = PixelFormat.Rgba;
        //            break;
        //        case 17:
        //            type = PixelInternalFormat.Rgba;
        //            utype = PixelFormat.Bgra;
        //            break;
        //        case 21:
        //            type = PixelInternalFormat.CompressedRedRgtc1;
        //            break;
        //        case 22:
        //            type = PixelInternalFormat.CompressedRgRgtc2;
        //            break;
        //        default:
        //            throw new NotImplementedException($"Unknown nut format {typet}");
        //    }
        //}

        //public int getNutFormat()
        //{
        //    switch (type)
        //    {
        //        case PixelInternalFormat.CompressedRgbaS3tcDxt1Ext:
        //            return 0;
        //        case PixelInternalFormat.CompressedRgbaS3tcDxt3Ext:
        //            return 1;
        //        case PixelInternalFormat.CompressedRgbaS3tcDxt5Ext:
        //            return 2;
        //        case PixelInternalFormat.Rgba:
        //            return (utype == PixelFormat.Rgba) ? 14 : 17;
        //        case PixelInternalFormat.CompressedRedRgtc1:
        //            return 21;
        //        case PixelInternalFormat.CompressedRgRgtc2:
        //            return 22;
        //        default:
        //            throw new NotImplementedException($"Unknown pixel format 0x{type:X}");
        //    }
        //}

        public byte[] Rebuild()
        {
            OutputBuffer o = new OutputBuffer();

            o.writeInt(0x4E545033); // "NTP3"
            o.writeShort(0x0200);
            o.writeShort(1);
            o.writeInt(0);
            o.writeInt(0);

            int size = data.Length;
            int headerSize = 0x60;

            // // headerSize 0x50 seems to crash with models
            //if (texture.mipmaps.Count == 1)
            //{
            //    headerSize = 0x50;
            //}

            o.writeInt(size + headerSize);
            o.writeInt(0x00);
            o.writeInt(size);
            o.writeShort((short)headerSize);
            o.writeShort(0);
            o.writeShort(1);
            o.writeShort((short)format); // dxt5
            o.writeShort((short)width);
            o.writeShort((short)height);
            o.writeInt(0);
            o.writeInt(0);
            o.writeInt(0x60);
            o.writeInt(0);
            o.writeInt(0);
            o.writeInt(0);

            o.writeInt(data.Length);
            o.writeInt(0);
            o.writeInt(0);
            o.writeInt(0);

            o.writeInt(0x65587400); // "eXt\0"
            o.writeInt(0x20);
            o.writeInt(0x10);
            o.writeInt(0x00);
            o.writeInt(0x47494458); // "GIDX"
            o.writeInt(0x10);
            o.writeInt(id);
            o.writeInt(0);

            o.write(data);

            return o.getBytes();
        }
    }
}
