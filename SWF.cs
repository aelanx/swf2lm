using ImageMagick;
using System;
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
                byte currByte = file.readByte();
                int bitsPerComponent = currByte >> 3;

                var values = new uint[4];

                for (int i = 5, currNum = 0; currNum < 4; currNum++)
                {
                    for (int exponent = bitsPerComponent - 1; exponent >= 0; exponent--)
                    {
                        var currBit = i % 8;
                        if (currBit == 0)
                            currByte = file.readByte();

                        values[currNum] |= (uint)(((currByte >> (7 - currBit)) & 1) << exponent);

                        i++;
                    }

                    values[currNum] /= 20;
                }

                minX = (int)values[0];
                maxX = (int)values[1];
                minY = (int)values[2];
                maxY = (int)values[3];
            }
        }

        static public List<int> ReadVector(InputBuffer file, int size)
        {
            byte currByte = file.readByte();
            int bitsPerComponent = currByte >> 3;

            var values = new List<int>();
            for (int i = 5, currNum = 0; currNum < size; currNum++)
            {
                int value = 0;

                for (int exponent = bitsPerComponent - 1; exponent >= 0; exponent--)
                {
                    var currBit = i % 8;
                    var thing = (currByte >> (7 - currBit)) & 1;
                    value |= thing << exponent;

                    if ((++i % 8) == 0)
                        currByte = file.readByte();
                }

                values.Add(value);
            }

            return values;
        }

        public class Matrix
        {
            public int[] data = new int[2 * 3];

            public Matrix() { }

            public Matrix(InputBuffer file)
            {
                byte currByte = file.readByte();
                bool hasScale = (currByte & 0x80) == 0x80;
                if (hasScale)
                {
                    var scale = ReadVector(file, 2);
                    var x = 43;
                }
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

                        data[i + 0] = b;
                        data[i + 1] = b;
                        data[i + 2] = b;
                        data[i + 3] = a; //
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
            lm.positions.Add(new Lumen.Vector2(rect.maxX/2, rect.maxY/2));

            Framerate = file.readShort(); // fixed point 8.8
            short frameCount = file.readShortLE();
            // end of header

            var textures = new List<Image>();

            while (true)
            {
                short typeAndSize = file.readShortLE();
                var type = (TagType)((typeAndSize & 0xFFC0) >> 6);

                int tagSize = (typeAndSize & 0x3F);
                if (tagSize == 0x3F)
                    tagSize = (int)file.readUIntLE();

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
                else
                {
                    var tag = new UnhandledTag();
                    tag.type = type;
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
            lm.properties.maxDepth = 0;
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
            lm.symbols.Add("init");
            lm.symbols.Add("in");
            lm.symbols.Add("in_end");
            lm.symbols.Add("out");
            lm.symbols.Add("out_end");
            lm.symbols.Add("00000");

            var atlas = new Lumen.TextureAtlas();
            atlas.id = 0;
            atlas.unk = 0x5B; // pool id
            atlas.width = 512;
            atlas.height = 512;
            lm.textureAtlases.Add(atlas);

            var sprite = new Lumen.Sprite();
            sprite.characterId = 3;
            sprite.unk1 = 0;
            sprite.unk2 = 0;
            sprite.unk3 = 0x10000;

            var placement = new Lumen.Sprite.Placement();
            placement.characterId = 1;
            placement.placementId = 0;
            placement.unk1 = 0;
            placement.nameId = 0;
            placement.unk2 = 1;
            placement.unk3 = 0;
            placement.depth = 0;
            placement.unk4 = 0;
            placement.transformFlags = 0;
            placement.transformId = 0;
            placement.positionFlags = 0x8000;
            placement.positionId = 0;
            placement.colorId1 = 0;
            placement.colorId2 = 1;


            for (int i = 0; i < 5; i++)
            {
                var label = new Lumen.Sprite.Label();
                label.nameId = 3 + i;
                label.startFrame = i;
                label.unk1 = 0;
                sprite.labels.Add(label);

                var frame = new Lumen.Sprite.Frame();
                frame.id = i;
                frame.placements.Add(placement);
                sprite.keyframes.Add(frame);
                if (i > 0)
                {
                    frame = new Lumen.Sprite.Frame();
                    frame.id = i;
                }
                sprite.frames.Add(frame);
            }

            lm.sprites.Add(sprite);

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
