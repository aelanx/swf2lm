using ImageMagick;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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

                file.alignBits();
            }
        }

        public class Matrix
        {
            public float[] data = new float[2 * 3];
            public bool hasScale;
            public bool hasRotation;

            public Matrix() { }

            public Matrix(InputBuffer file, int size = -1)
            {
                hasScale = (file.readBit() == 1);
                if (hasScale)
                {
                    var numScaleBits = file.readBits(5);

                    data[0] = file.readSignedBits((int)numScaleBits) * (1.0f / (1 << 16));
                    data[3] = file.readSignedBits((int)numScaleBits) * (1.0f / (1 << 16));
                }
                else
                {
                    data[0] = data[3] = 1.0f;
                }

                hasRotation = (file.readBit() == 1);
                if (hasRotation)
                {
                    var numRotateBits = file.readBits(5);

                    data[1] = file.readSignedBits((int)numRotateBits) * (1.0f / (1 << 16));
                    data[2] = file.readSignedBits((int)numRotateBits) * (1.0f / (1 << 16));
                }
                else
                {
                    data[1] = data[2] = 0.0f;
                }

                var numTranslateBits = file.readBits(5);
                data[4] = file.readSignedBits((int)numTranslateBits) / 20;
                data[5] = file.readSignedBits((int)numTranslateBits) / 20;

                file.alignBits();
            }

        }

        public abstract class Tag
        {
            public abstract void Read(InputBuffer file, TagType type, int size);
        }

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
            public const byte PlaceFlagHasClipActions = 0x80;
            public const byte PlaceFlagHasClipDepth = 0x40;
            public const byte PlaceFlagHasName = 0x20;
            public const byte PlaceFlagHasRatio = 0x10;
            public const byte PlaceFlagHasColorTransform = 0x08;
            public const byte PlaceFlagHasMatrix = 0x04;
            public const byte PlaceFlagHasCharacter = 0x02;
            public const byte PlaceFlagMove = 0x01;

            public short depth;
            public short characterId = -1;
            public string name = null;
            public Matrix matrix = null;
            public short ratio;
            public short clipDepth = -1;

            public PlaceObject2() { }
            public PlaceObject2 (InputBuffer file, TagType type, int size)
            {
                Read(file, type, size);
            }

            public override void Read(InputBuffer file, TagType type, int size)
            {
                byte littleBits = file.readByte();
                depth = file.readShortLE();

                if ((littleBits & PlaceFlagHasCharacter) == PlaceFlagHasCharacter)
                    characterId = file.readShortLE();

                if ((littleBits & PlaceFlagHasMatrix) == PlaceFlagHasMatrix)
                    matrix = new Matrix(file, size*8);

                if ((littleBits & PlaceFlagHasColorTransform) == PlaceFlagHasColorTransform)
                {
                    var zzz = 3;
                    // var colorXform = new CXFORMWITHALPHA(file);
                }

                if ((littleBits & PlaceFlagHasRatio) == PlaceFlagHasRatio)
                    ratio = file.readShortLE();

                if ((littleBits & PlaceFlagHasName) == PlaceFlagHasName)
                    name = file.readString();

                if ((littleBits & PlaceFlagHasClipDepth) == PlaceFlagHasClipDepth)
                    clipDepth = file.readShortLE();

                if ((littleBits & PlaceFlagHasClipActions) == PlaceFlagHasClipActions)
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

                byte numFillAndLineBits = file.readByte();
                
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
                //alphaData = file.read(bmp.Width * bmp.Height);

                //var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                for (int x = 0; x < bmp.Width; x++)
                {
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        var px = bmp.GetPixel(x, y);
                        var newPx = Color.FromArgb(alphaData[y * bmp.Width + x], px.R, px.G, px.B);
                        bmp.SetPixel(x, y, newPx);
                    }
                }
                //bmp.UnlockBits(bmpData);

                var fn = $@"C:\s4explore\workspace\content\patch\data\ui\lumen\main\img-{0:D5}.png";
                bmp.Save(fn);
                //alphaData = (file.ptr - dataSize - 2)
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
                        data[i + 3] = a;// a;
                    }

                    var image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    var bmpData = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                    Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
                    image.UnlockBits(bmpData);
                    Image = image;
                }

                var x = 5;
            }
        }

        public class UnhandledTag : Tag
        {
            public TagType type;
            public byte[] data;
            public int offset;

            public override void Read(InputBuffer file, TagType type, int size)
            {
                throw new NotImplementedException();
            }
        }

        public Rect rect;
        public short Framerate;
        public List<Tag> Tags = new List<Tag>();

        public SWF() { }

        public SWF (string filename)
        {
            var file = new InputBuffer(filename);
            var lm = new Lumen();

            lm.colors.Add(new Lumen.Color(0x100, 0x100, 0x100, 0x100));
            lm.colors.Add(new Lumen.Color(0, 0, 0, 0));

            byte magic0 = file.readByte();
            short magicPad = file.readShort();
            byte version = file.readByte();

            uint length = file.readUIntLE();

            rect = new Rect(file);

            Framerate = file.readShort(); // fixed point 8.8
            short frameCount = file.readShortLE();
            // end of header

            var textures = new List<Image>();

            var sprite = new Lumen.Sprite();
            sprite.characterId = 3;
            sprite.unk1 = 0;
            sprite.unk2 = 0;
            sprite.unk3 = 0x10000;
            lm.sprites.Add(sprite);

            while (true)
            {
                short typeAndSize = file.readShortLE();
                var type = (TagType)((typeAndSize & 0xFFC0) >> 6);

                int tagSize = (typeAndSize & 0x3F);
                if (tagSize == 0x3F)
                    tagSize = (int)file.readUIntLE();

                //if (type == TagType.DefineBitsJPEG3)
                //{
                //    var tag = new DefineBitsJPEG3(file, type, tagSize);

                //    Tags.Add(tag);
                //}
                if (type == TagType.DefineBitsLossless || type == TagType.DefineBitsLossless2)
                {
                    var tag = new DefineBitsLossless(file, type, tagSize);
                    Tags.Add(tag);
                    textures.Add(tag.Image);
                    var img = tag.Image;

                    var shape = new Lumen.Shape();
                    shape.characterId = tag.CharacterId;
                    var graphic = new Lumen.Graphic();
                    graphic.atlasId = 0;
                    graphic.fillType = Lumen.Graphic.FillType.ClippedBitmap;
                    graphic.indices = new ushort[6]
                    {
                        0, 1, 2,
                        3, 0, 2
                    };
                    float hw = img.Width / 2.0f;
                    float hh = img.Height / 2.0f;
                    graphic.verts = new Lumen.Vertex[4]
                    {
                        new Lumen.Vertex(-hw,  hh, 0, 1),
                        new Lumen.Vertex(-hw, -hh, 0, 0),
                        new Lumen.Vertex( hw, -hh, 1, 0),
                        new Lumen.Vertex( hw,  hh, 1, 1)
                    };
                    shape.graphics = new Lumen.Graphic[] { graphic };

                    lm.bounds.Add(new Lumen.ShapeBounds(-hw, -hh, hw, hh));

                    lm.shapes.Add(shape);
                }
                //else if (type == TagType.DefineShape)
                //{
                //    Tags.Add(new DefineShape(file, type, tagSize));
                //}
                else if (type == TagType.PlaceObject2)
                {
                    var tag = new PlaceObject2(file, type, tagSize);
                    Tags.Add(tag);

                    var placement = new Lumen.Sprite.Placement();
                    if (tag.characterId == -1)
                    {
                        placement.characterId = 0;
                        placement.placementId = -1;
                        placement.unk2 = 2;
                    }
                    else
                    {
                        //placement.characterId = tag.characterId;
                        placement.characterId = 1;
                        placement.placementId = 0;
                        placement.unk2 = 1;
                    }

                    //placement.depth = tag.depth;
                    placement.depth = 0;
                    placement.unk1 = 0;
                    placement.nameId = 0;
                    placement.unk3 = 0;
                    placement.unk4 = 0;
                    placement.transformFlags = 0;
                    placement.transformId = 0;

                    // use position entry if no scale or rotation
                    if (!tag.matrix.hasRotation && !tag.matrix.hasScale)
                    {
                        placement.positionFlags = 0x8000;
                        placement.positionId = (short)lm.positions.Count;
                        lm.positions.Add(new Lumen.Vector2(tag.matrix.data[4], tag.matrix.data[5]));
                    }
                    else
                    {
                        placement.positionFlags = 0;
                        placement.positionId = (short)lm.transforms.Count;
                        var xform = new Lumen.Transform();
                        xform.M11 = tag.matrix.data[0];
                        xform.M12 = tag.matrix.data[1];
                        xform.M21 = tag.matrix.data[2];
                        xform.M22 = tag.matrix.data[3];
                        xform.M31 = tag.matrix.data[4];
                        xform.M32 = tag.matrix.data[5];
                        lm.transforms.Add(xform);

                    }
                    placement.colorId1 = 0;
                    placement.colorId2 = 1;

                    var frame = new Lumen.Sprite.Frame();
                    frame.id = lm.sprites[0].frames.Count;
                    frame.placements.Add(placement);
                    lm.sprites[0].frames.Add(frame);
                }
                else
                {
                    var tag = new UnhandledTag();
                    tag.type = type;
                    tag.offset = (int)file.ptr;
                    tag.data = file.read(tagSize);
                    Tags.Add(tag);
                }


                if (type == TagType.End)
                    break;
            }

            var packer = new TexturePacker();
            packer.Process(textures);

            lm.unkF008 = new Lumen.UnhandledTag(Lumen.TagType.UnkF008, 1, new byte[] { 0, 0, 0, 0 });
            lm.unkF009 = new Lumen.UnhandledTag(Lumen.TagType.UnkF009, 1, new byte[] { 0, 0, 0, 0 });
            lm.unkF00A = new Lumen.UnhandledTag(Lumen.TagType.UnkF00A, 1, new byte[] { 0, 0, 0, 0 });
            lm.unk000A = new Lumen.UnhandledTag(Lumen.TagType.Fonts, 1, new byte[] { 0, 0, 0, 0 });
            lm.unkF00B = new Lumen.UnhandledTag(Lumen.TagType.UnkF00B, 1, new byte[] { 0, 0, 0, 1 });

            lm.properties = new Lumen.Properties();
            lm.properties.unk0 = 0;
            lm.properties.unk1 = 1;
            lm.properties.unk2 = 2;
            lm.properties.maxCharacterId = 3;
            lm.properties.unk4 = -1;
            lm.properties.maxCharacterId2 = 3;
            lm.properties.maxDepth = 1; // FIXME
            lm.properties.unk7 = 0;
            lm.properties.framerate = Framerate;
            lm.properties.width = rect.maxX;
            lm.properties.height = rect.maxY;
            lm.properties.unk8 = 0;
            lm.properties.unk9 = 0;

            lm.properties2 = new Lumen.Properties2();
            lm.properties2.numShapes = 1;
            lm.properties2.numSprites = 1;
            lm.properties2.numTexts = 1;

            lm.header = new Lumen.Header();
            lm.header.magic = 0x4C4D4200;
            lm.header.unk0 = 0x00000010;
            lm.header.unk1 = 0x10010200;
            lm.header.unk2 = 0;
            lm.header.unk3 = 2;
            lm.header.unk4 = 0;
            lm.header.unk5 = 0;
            lm.header.filesize = 0;
            lm.header.unk6 = 0;
            lm.header.unk7 = 0;
            lm.header.unk8 = 0;
            lm.header.unk9 = 0;
            lm.header.unk10 = 0;
            lm.header.unk11 = 0;
            lm.header.unk12 = 0;
            lm.header.unk13 = 0;

            lm.symbols.Add("");
            lm.symbols.Add("lmf");
            lm.symbols.Add("15");
            lm.symbols.Add("00000");

            var atlas = new Lumen.TextureAtlas();
            atlas.id = 0;
            atlas.unk = 0x08;
            atlas.width = 512;
            atlas.height = 512;
            lm.textureAtlases.Add(atlas);

            using (var fs = new FileStream(@"C:\s4explore\workspace\content\patch\data\ui\lumen\main\main.lm", FileMode.Create))
            {
                var lmData = lm.Rebuild();
                lm.header.filesize = lmData.Length;
                lmData = lm.Rebuild();
                fs.Write(lmData, 0, lmData.Length);
            }

            var reLm = new Lumen(@"C:\s4explore\workspace\content\patch\data\ui\lumen\main\main.lm");

            var x = 4;
        }
    }
}
