using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace swf2lm
{
    class SWF
    {
        public enum TagType : short
        {
            End = 0,
            ShowFrame = 1,
            DefineShape = 2,
            PlaceObject = 4,
            RemoveObject = 5,
            DefineBits = 6,
            DefineButton = 7,
            JPEGTables = 8,
            SetBackgroundColor = 9,
            DefineFont = 10,
            DefineText = 11,
            DoAction = 12,
            DefineFontInfo = 13,
            DefineSound = 14,
            StartSound = 15,
            DefineButtonSound = 17,
            SoundStreamHead = 18,
            SoundStreamBlock = 19,
            DefineBitsLossless = 20,
            DefineBitsJPEG2 = 21,
            DefineShape2 = 22,
            DefineButtonCxform = 23,
            Protect = 24,
            PlaceObject2 = 26,
            RemoveObject2 = 28,
            DefineShape3 = 32,
            DefineText2 = 33,
            DefineButton2 = 34,
            DefineBitsJPEG3 = 35,
            DefineBitsLossless2 = 36,
            DefineEditText = 37,
            DefineSprite = 39,
            FrameLabel = 43,
            SoundStreamHead2 = 45,
            DefineMorphShape = 46,
            DefineFont2 = 48,
            ExportAssets = 56,
            ImportAssets = 57,
            EnableDebugger = 58,
            DoInitAction = 59,
            DefineVideoStream = 60,
            VideoFrame = 61,
            DefineFontInfo2 = 62,
            EnableDebugger2 = 64,
            ScriptLimits = 65,
            SetTabIndex = 66,
            FileAttributes = 69,
            PlaceObject3 = 70,
            ImportAssets2 = 71,
            DefineFontAlignZones = 73,
            CSMTextSettings = 74,
            DefineFont3 = 75,
            SymbolClass = 76,
            Metadata = 77,
            DefineScalingGrid = 78,
            DoABC = 82,
            DefineShape4 = 83,
            DefineMorphShape2 = 84,
            DefineSceneAndFrameLabelData = 86,
            DefineBinaryData = 87,
            DefineFontName = 88,
            StartSound2 = 89,
            DefineBitsJPEG4 = 90,
            DefineFont4 = 91,
            EnableTelemetry = 93
        }

        public class Rect
        {
            public int minX;
            public int maxX;
            public int minY;
            public int maxY;

            public Rect() { }

            public Rect(InputBuffer file)
            {
                int bitsPerComponent = (int)file.readBits(5);
                minX = file.readSignedBits(bitsPerComponent) / 20;
                maxX = file.readSignedBits(bitsPerComponent) / 20;
                minY = file.readSignedBits(bitsPerComponent) / 20;
                maxY = file.readSignedBits(bitsPerComponent) / 20;

                file.AlignBits();
            }
        }

        public class Matrix
        {
            public float[] data = new float[2 * 3];
            public bool hasScale;
            public bool hasRotation;

            public static Matrix Identity = new Matrix(new float[] { 1, 0, 0, 1, 0, 0 });

            public Matrix() { }

            public Matrix(float[] data)
            {
                this.data = data;
            }

            public Matrix(InputBuffer file, int size = -1)
            {
                hasScale = (file.readBit() == 1);
                if (hasScale)
                {
                    var numScaleBits = file.readBits(5);
                    data[0] = file.readFixed16((int)numScaleBits);
                    data[3] = file.readFixed16((int)numScaleBits);
                }
                else
                {
                    data[0] = data[3] = 1.0f;
                }

                hasRotation = (file.readBit() == 1);
                if (hasRotation)
                {
                    var numRotateBits = file.readBits(5);
                    data[1] = file.readFixed16((int)numRotateBits);
                    data[2] = file.readFixed16((int)numRotateBits);
                }
                else
                {
                    data[1] = data[2] = 0.0f;
                }

                var numTranslateBits = file.readBits(5);
                data[4] = file.readSignedBits((int)numTranslateBits) / 20;
                data[5] = file.readSignedBits((int)numTranslateBits) / 20;

                file.AlignBits();
            }

        }

        public class CXformWithAlpha
        {
            public float[] Data = new float[2 * 3];
            public bool HasAdd;
            public bool HasMult;
            public Lumen.Color Add;
            public Lumen.Color Mult;

            public CXformWithAlpha() { }

            public CXformWithAlpha(InputBuffer file, int size = -1)
            {
                Mult = new Lumen.Color(0xFFFFFFFF);
                Add = new Lumen.Color(0x00000000);

                HasAdd = (file.readBit() == 1);
                HasMult = (file.readBit() == 1);

                var numBits = file.readBits(4);
                if (HasMult)
                {
                    Mult.R = file.readFixed8((int)numBits);
                    Mult.G = file.readFixed8((int)numBits);
                    Mult.B = file.readFixed8((int)numBits);
                    Mult.A = file.readFixed8((int)numBits);
                }

                if (HasAdd)
                {
                    Add.R = file.readFixed8((int)numBits);
                    Add.G = file.readFixed8((int)numBits);
                    Add.B = file.readFixed8((int)numBits);
                    Add.A = file.readFixed8((int)numBits);
                }

                file.AlignBits();
            }

        }

        public abstract class Tag
        {
            public abstract void Read(InputBuffer file, TagType type, int size);
        }

        public class NullTag : Tag
        {
            public override void Read(InputBuffer file, TagType type, int size)
            {

            }
        }

        public class DefineSprite : Tag
        {
            public short CharacterId;

            public DefineSprite() { }

            public DefineSprite(InputBuffer file, TagType type, int size)
            {
                Read(file, type, size);
            }

            public override void Read(InputBuffer file, TagType type, int size)
            {
                CharacterId = file.readShortLE();
                var frameCount = file.readShortLE();
                file.ptr += 0x0C;
            }
        }

        public class FrameLabel : Tag
        {
            public string name;

            public FrameLabel() { }

            public FrameLabel(InputBuffer file, TagType type, int size)
            {
                Read(file, type, size);
            }

            public override void Read(InputBuffer file, TagType type, int size)
            {
                name = file.readString();
                file.ptr++;
            }
        }

        public class ShowFrame : NullTag { }
        public class End : NullTag { }

        public enum FillStyleType : byte
        {
            Solid = 0x00,
            LinearGradient = 0x10,
            RadialGradient = 0x12,
            FocalRadialGradient = 0x13,
            RepeatingBitmap = 0x40,
            ClippedBitmap = 0x41,
            NonSmoothedRepeatingBitmap = 0x42,
            NonSmoothedClippedBitmap = 0x43
        }

        public class PlaceObject2 : Tag
        {
            public enum PlaceFlag : byte
            { 
                Move = 0x01,
                HasCharacter = 0x02,
                HasMatrix = 0x04,
                HasColorTransform = 0x08,
                HasRatio = 0x10,
                HasName = 0x20,
                HasClipDepth = 0x40,
                HasClipActions = 0x80
            }

            public short depth;
            public short characterId = -1;
            public string name = null;
            public Matrix matrix = null;
            public CXformWithAlpha colorXform = null;
            public short ratio;
            public short clipDepth = -1;

            public PlaceObject2() { }
            public PlaceObject2 (InputBuffer file, TagType type, int size)
            {
                Read(file, type, size);
            }

            public override void Read(InputBuffer file, TagType type, int size)
            {
                var flags = (PlaceFlag)file.readByte();
                depth = file.readShortLE();

                if ((flags & PlaceFlag.HasCharacter) == PlaceFlag.HasCharacter)
                    characterId = file.readShortLE();

                if ((flags & PlaceFlag.HasMatrix) == PlaceFlag.HasMatrix)
                    matrix = new Matrix(file, size * 8);
                else
                    matrix = Matrix.Identity;

                if ((flags & PlaceFlag.HasColorTransform) == PlaceFlag.HasColorTransform)
                    colorXform = new CXformWithAlpha(file);

                if ((flags & PlaceFlag.HasRatio) == PlaceFlag.HasRatio)
                    ratio = file.readShortLE();

                if ((flags & PlaceFlag.HasName) == PlaceFlag.HasName)
                    name = file.readString();

                if ((flags & PlaceFlag.HasClipDepth) == PlaceFlag.HasClipDepth)
                    clipDepth = file.readShortLE();

                if ((flags & PlaceFlag.HasClipActions) == PlaceFlag.HasClipActions)
                {
                    var whatDoYouThinkTapWaterIs = "it's a gay-bomb, baby!";
                }
            }
        }

        public class DefineShape : Tag
        {
            public short shapeId;
            public Rect bounds;

            public DefineShape(InputBuffer file, TagType type, int size)
            {
                Read(file, type, size);
            }

            public override void Read(InputBuffer file, TagType type, int size)
            {
                shapeId = file.readShortLE();
                bounds = new Rect(file);
                int fillStyleCount = file.readByte();
                if (fillStyleCount == 0xFF)
                    fillStyleCount = file.readShortLE();

                for (int i = 0; i < fillStyleCount; i++)
                {
                    var fillStyleType = (FillStyleType)file.readByte();

                    switch (fillStyleType)
                    {
                        case FillStyleType.Solid:
                        {
                            if (type == TagType.DefineShape3)
                                file.skip(4); // RGBA
                            else
                                file.skip(3); // RGB
                        } break;

                        case FillStyleType.LinearGradient:
                        case FillStyleType.RadialGradient:
                        case FillStyleType.FocalRadialGradient:
                        {
                            throw new NotImplementedException();

                            var gradMat = new Matrix(file);

                            if (fillStyleType == FillStyleType.FocalRadialGradient)
                            {
                                // FOCALGRADIENT
                            }
                            else
                            {
                                // GRADIENT
                            }
                        } break;

                        case FillStyleType.RepeatingBitmap:
                        case FillStyleType.ClippedBitmap:
                        case FillStyleType.NonSmoothedRepeatingBitmap:
                        case FillStyleType.NonSmoothedClippedBitmap:
                        {
                            short bitmapId = file.readShortLE();
                            var bitmapMat = new Matrix(file);
                        } break;
                    }
                }

                uint numFillBits = file.readBits(4);
                uint numLineBits = file.readBits(4);
                
                //file.skip((uint)size);
            }
        }

        public class DefineBitsJPEG3 : Tag
        {
            public short CharacterId;
            public Image Image;

            public byte[] data;
            public byte[] alphaData;

            public DefineBitsJPEG3() { }

            public DefineBitsJPEG3(InputBuffer file, TagType type, int size)
            {
                Read(file, type, size);
            }

            public override void Read(InputBuffer file, TagType type, int size)
            {
                CharacterId = file.readShortLE();
                uint dataSize = file.readUIntLE();
                data = file.read((int)dataSize);
                var stream = new MemoryStream(data);
                var bmp = new Bitmap(stream);

                file.ptr += 2;
                var compressedAlphaStream = new MemoryStream(file.read((int)(size - (dataSize + 2 + 2))));
                var decompressedAlpha = new MemoryStream();

                var ds = new DeflateStream(compressedAlphaStream, CompressionMode.Decompress);
                ds.CopyTo(decompressedAlpha);

                alphaData = decompressedAlpha.ToArray();

                for (int x = 0; x < bmp.Width; x++)
                {
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        var px = bmp.GetPixel(x, y);
                        var newPx = Color.FromArgb(alphaData[y * bmp.Width + x], px.R, px.G, px.B);
                        bmp.SetPixel(x, y, newPx);
                    }
                }
                
                //var fn = $@"C:\s4explore\workspace\content\patch\data\ui\lumen\main\img-{0:D5}.png";
                //bmp.Save(fn);
                Image = bmp;
            }
        }

        public class DefineBitsLossless : Tag
        {
            public enum BitmapFormat : byte
            {
                ColorMap = 3,
                ARGB15 = 4,
                ARGB24 = 5
            }

            public short CharacterId;
            public Image Image;

            public DefineBitsLossless() { }

            public DefineBitsLossless(InputBuffer file, TagType type, int size)
            {
                Read(file, type, size);
            }

            public override void Read(InputBuffer file, TagType type, int size)
            {
                CharacterId = file.readShortLE();
                var bitmapFormat = (BitmapFormat)file.readByte();
                short width = file.readShortLE();
                short height = file.readShortLE();
                byte[] data;

                byte colorTableSize;
                if (bitmapFormat == BitmapFormat.ColorMap)
                {
                    colorTableSize = file.readByte();

                    file.read(size - (2 + 1 + 2 + 2 + 1));
                }
                else
                {
                    file.skip(0x02); // c# deflate no likey magic :(
                    var compressedStream = new MemoryStream(file.read(size - (2 + 1 + 2 + 2 + 2)));
                    var decompressedStream = new MemoryStream();

                    var ds = new DeflateStream(compressedStream, CompressionMode.Decompress);
                    ds.CopyTo(decompressedStream);

                    data = decompressedStream.ToArray();

                    // FIXME:
                    for (int i = 0; i < data.Length; i += 4)
                    {
                        byte a = data[i + 0];
                        byte r = data[i + 1];
                        byte g = data[1 + 2];
                        byte b = data[i + 3];

                        data[i + 0] = b; // blue
                        data[i + 1] = b; // green
                        data[i + 2] = b; // red
                        data[i + 3] = a; // a
                    }

                    var image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    var bmpData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                    Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
                    image.UnlockBits(bmpData);
                    Image = image;
                }
            }
        }

        public class UnhandledTag : Tag
        {
            public TagType type;
            public byte[] data;
            public int offset;

            public UnhandledTag()
            {
            }

            public UnhandledTag(InputBuffer file, TagType type, int size)
            {
                Read(file, type, size);
            }

            public override void Read(InputBuffer file, TagType type, int size)
            {
                this.type = type;
                offset = (int)file.ptr;
                data = file.read(size);
            }
        }

        public Rect rect;
        public short Framerate;
        public List<Tag> Tags = new List<Tag>();

        public SWF() { }

        public SWF (string filename)
        {
            var file = new InputBuffer(filename);

            byte magic0 = file.readByte();


            short magicPad = file.readShort();
            byte version = file.readByte();
            if (version > 8)
                throw new Exception($"Unsupported SWF version {version}. Maximum supported verison = 8.");

            uint length = file.readUIntLE();


            // lzma. no point in supporting since it's only used in swf 13+
            // which will cause other problems I don't care to deal with.
            //if (magic0 == 'Z')
            //    throw new Exception("LZMA");

            if (magic0 == 'C')
            {
                file.ptr += 2;
                var compressedStream = new MemoryStream(file.read(file.Length - 10));
                var decompressedStream = new MemoryStream();

                var ds = new DeflateStream(compressedStream, CompressionMode.Decompress);
                ds.CopyTo(decompressedStream);

                file = new InputBuffer(decompressedStream.ToArray());
            }

            rect = new Rect(file);
            Framerate = file.readShort(); // fixed point 8.8
            short frameCount = file.readShortLE();
            // end of header

            while (true)
            {
                short typeAndSize = file.readShortLE();
                var type = (TagType)((typeAndSize & 0xFFC0) >> 6);

                int tagSize = (typeAndSize & 0x3F);
                if (tagSize == 0x3F)
                    tagSize = (int)file.readUIntLE();

                switch (type)
                {
                    case TagType.DefineBitsLossless:
                    case TagType.DefineBitsLossless2:
                        Tags.Add(new DefineBitsLossless(file, type, tagSize));
                        break;
                    //case TagType.DefineBitsJPEG3:
                    //    Tags.Add(new DefineBitsJPEG3(file, type, tagSize));
                    //    break;
                    case TagType.DefineSprite:
                        Tags.Add(new DefineSprite(file, type, tagSize));
                        break;
                    //case TagType.DefineShape:
                    //    Tags.Add(new DefineShape(file, type, tagSize));
                    //    break;
                    case TagType.FrameLabel:
                        Tags.Add(new FrameLabel(file, type, tagSize));
                        break;
                    case TagType.ShowFrame:
                        Tags.Add(new ShowFrame());
                        break;
                    case TagType.PlaceObject2:
                        Tags.Add(new PlaceObject2(file, type, tagSize));
                        break;
                    case TagType.End:
                        Tags.Add(new End());
                        break;
                    default:
                        Tags.Add(new UnhandledTag(file, type, tagSize));
                        break;
                }

                if (type == TagType.End)
                    break;
            }
        }
    }
}
