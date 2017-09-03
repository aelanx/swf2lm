using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace swf2lm
{
    public class Lumen
    {
        public enum TagType : int
        {
            Invalid = 0x0000,

            Fonts = 0x000A,
            Symbols = 0xF001,
            Colors = 0xF002,
            Transforms = 0xF003,
            Bounds = 0xF004,
            ActionScript = 0xF005,
            TextureAtlases = 0xF007,
            UnkF008 = 0xF008,
            UnkF009 = 0xF009,
            UnkF00A = 0xF00A,
            UnkF00B = 0xF00B,
            Properties = 0xF00C,
            Properties2 = 0xF00D,

            Shape = 0xF022,
            Graphic = 0xF024,
            ColorMatrix = 0xF037,
            Positions = 0xF103,

            DynamicText = 0x0025,
            DefineSprite = 0x0027,

            FrameLabel = 0x002B,
            ShowFrame = 0x0001,
            Keyframe = 0xF105,
            PlaceObject = 0x0004,
            RemoveObject = 0x0005,
            DoAction = 0x000C,

            End = 0xFF00
        }

        public class UnhandledTag
        {
            public UnhandledTag()
            {
                type = TagType.Invalid;
            }

            public UnhandledTag(TagType type, int size, InputBuffer f)
            {
                this.type = type;
                this.size = size;
                this.data = f.read(size * 4);
            }

            public UnhandledTag(TagType type, int size, byte[] data)
            {
                this.type = type;
                this.size = size;
                this.data = data;
            }

            public UnhandledTag(InputBuffer f)
            {
                type = (TagType)f.readInt();
                size = f.readInt();
                data = f.read(size * 4);
            }

            public void Write(OutputBuffer o)
            {
                o.writeInt((int)type);
                o.writeInt(size);
                o.write(data);
            }

            public TagType type;
            public int size;

            byte[] data;
        }

        public class Properties
        {
            public uint unk0;
            public uint unk1;
            public uint unk2;
            public uint maxCharacterId;
            public int unk4;
            public uint maxCharacterId2;
            public ushort maxDepth;
            public ushort unk7;
            public float framerate;
            public float width;
            public float height;
            public uint unk8;
            public uint unk9;

            public Properties() { }

            public Properties(InputBuffer f)
            {
                Read(f);
            }

            public void Read(InputBuffer f)
            {
                unk0 = (uint)f.readInt();
                unk1 = (uint)f.readInt();
                unk2 = (uint)f.readInt();
                maxCharacterId = (uint)f.readInt();
                unk4 = f.readInt();
                maxCharacterId2 = (uint)f.readInt();
                maxDepth = (ushort)f.readShort();
                unk7 = (ushort)f.readShort();
                framerate = f.readFloat();
                width = f.readFloat();
                height = f.readFloat();
                unk8 = (uint)f.readInt();
                unk9 = (uint)f.readInt();
            }

            public void Write(OutputBuffer o)
            {
                o.writeInt((int)TagType.Properties);
                o.writeInt(12);

                o.writeInt((int)unk0);
                o.writeInt((int)unk1);
                o.writeInt((int)unk2);
                o.writeInt((int)maxCharacterId);
                o.writeInt(unk4);
                o.writeInt((int)maxCharacterId2);
                o.writeShort((short)maxDepth);
                o.writeShort((short)unk7);
                o.writeFloat(framerate);
                o.writeFloat(width);
                o.writeFloat(height);
                o.writeInt((int)unk8);
                o.writeInt((int)unk9);
            }
        }

        public class Properties2
        {
            public uint numShapes;
            public uint unk1;
            public uint numSprites;
            public uint unk3;
            public uint numTexts;
            public uint unk5;
            public uint unk6;
            public uint unk7;

            public Properties2() { }

            public Properties2(InputBuffer f)
            {
                Read(f);
            }

            public void Read(InputBuffer f)
            {
                numShapes = (uint)f.readInt();
                unk1 = (uint)f.readInt();
                numSprites = (uint)f.readInt();
                unk3 = (uint)f.readInt();
                numTexts = (uint)f.readInt();
                unk5 = (uint)f.readInt();
                unk6 = (uint)f.readInt();
                unk7 = (uint)f.readInt();
            }

            public void Write(OutputBuffer o)
            {
                o.writeInt((int)TagType.Properties2);
                o.writeInt(8);

                o.writeInt((int)numShapes);
                o.writeInt((int)unk1);
                o.writeInt((int)numSprites);
                o.writeInt((int)unk3);
                o.writeInt((int)numTexts);
                o.writeInt((int)unk5);
                o.writeInt((int)unk6);
                o.writeInt((int)unk7);
            }
        }

        public class Color
        {
            public float R = 0;
            public float G = 0;
            public float B = 0;
            public float A = 0;

            public Color()
            {
            }

            public Color(float r, float g, float b, float a)
            {
                this.R = r;
                this.G = g;
                this.B = b;
                this.A = a;
            }

            public Color(short r, short g, short b, short a)
            {
                this.R = r / 256.0f;
                this.G = g / 256.0f;
                this.B = b / 256.0f;
                this.A = a / 256.0f;
            }

            public Color(uint rgba)
            {
                R = ((rgba >> 24) & 0xFF) / 255.0f;
                G = ((rgba >> 16) & 0xFF) / 255.0f;
                B = ((rgba >> 8) & 0xFF) / 255.0f;
                A = ((rgba) & 0xFF) / 255.0f;
            }

            public override string ToString()
            {
                return $"[{R}, {G}, {B}, {A}]";
            }

            public override bool Equals(object obj)
            {
                var other = (Color)obj;
                return (R == other.R && G == other.G && B == other.B && A == other.A);
            }

            public override int GetHashCode()
            {
                return (int)R ^ (int)G ^ (int)B ^ (int)A;
            }
        }

        public class ShapeBounds
        {
            public ShapeBounds()
            {

            }

            public ShapeBounds(float l, float t, float r, float b)
            {
                left = l;
                top = t;
                right = r;
                bottom = b;
            }

            public float left;
            public float top;
            public float right;
            public float bottom;
        }

        public struct TextureAtlas
        {
            public int id;
            public int nameId;

            public float width;
            public float height;
        }

        public struct Vertex
        {
            public float x;
            public float y;
            public float u;
            public float v;

            public Vertex(float x, float y, float u, float v)
            {
                this.x = x;
                this.y = y;
                this.u = u;
                this.v = v;
            }
        }

        public class Graphic
        {
            public enum FillType : short
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

            public int nameId;
            public int atlasId;
            public FillType fillType;

            public Vertex[] verts;
            public ushort[] indices;

            public Graphic()
            {

            }

            public Graphic(InputBuffer f)
            {
                Read(f);
            }

            public void Read(InputBuffer f)
            {
                atlasId = f.readInt();
                fillType = (FillType)f.readShort();

                int numVerts = f.readShort();
                int numIndices = f.readInt();

                verts = new Vertex[numVerts];
                indices = new ushort[numIndices];

                for (int i = 0; i < numVerts; i++)
                {
                    verts[i] = new Vertex();
                    verts[i].x = f.readFloat();
                    verts[i].y = f.readFloat();
                    verts[i].u = f.readFloat();
                    verts[i].v = f.readFloat();
                }

                for (int i = 0; i < numIndices; i++)
                {
                    indices[i] = (ushort)f.readShort();
                }

                // indices are padded to word boundaries
                if ((numIndices % 2) != 0)
                {
                    f.skip(0x02);
                }
            }

            public void Write(OutputBuffer o)
            {
                OutputBuffer tag = new OutputBuffer();
                tag.writeInt(atlasId);
                tag.writeShort((short)fillType);
                tag.writeShort((short)verts.Length);
                tag.writeInt(indices.Length);

                foreach (var vert in verts)
                {
                    tag.writeFloat(vert.x);
                    tag.writeFloat(vert.y);
                    tag.writeFloat(vert.u);
                    tag.writeFloat(vert.v);
                }

                foreach (var index in indices)
                {
                    tag.writeShort((short)index);
                }

                if ((indices.Length % 2) != 0)
                {
                    tag.writeShort(0);
                }

                o.writeInt((int)TagType.Graphic);
                o.writeInt(tag.Size / 4);
                o.write(tag);
            }
        }

        public class Shape
        {
            public int characterId;
            public int unk1;
            public int texlistEntry;
            public int unk2;

            public Graphic[] graphics;

            public Shape()
            {

            }

            public Shape(InputBuffer f)
            {
                Read(f);
            }

            public void Read(InputBuffer f)
            {
                characterId = f.readInt();
                unk1 = f.readInt();
                texlistEntry = f.readInt();
                unk2 = f.readInt();

                int numGraphics = f.readInt();
                graphics = new Graphic[numGraphics];

                for (int i = 0; i < numGraphics; i++)
                {
                    f.skip(0x08); // graphic tag header
                    graphics[i] = new Graphic(f);
                }
            }

            public void Write(OutputBuffer o)
            {
                o.writeInt((int)TagType.Shape);
                o.writeInt(5);

                o.writeInt(characterId);
                o.writeInt(unk1);
                o.writeInt(texlistEntry);
                o.writeInt(unk2);
                o.writeInt(graphics.Length);

                foreach (var graphic in graphics)
                {
                    graphic.Write(o);
                }
            }
        }

        public struct DynamicText
        {
            public enum Alignment : short
            {
                Left = 0,
                Right = 1,
                Center = 2
            }

            public int characterId;
            public int unk1;
            public int placeholderTextId;
            public string placeholder; // FIXME
            public int unk2;
            public int colorId;
            public int unk3;
            public int unk4;
            public int unk5;
            public Alignment alignment;
            public short unk6;
            public int unk7;
            public int unk8;
            public float size;
            public int unk9;
            public int unk10;
            public int unk11;
            public int unk12;

            public DynamicText(InputBuffer f) : this()
            {
                Read(f);
            }

            public void Read(InputBuffer f)
            {
                characterId = f.readInt();
                unk1 = f.readInt();
                placeholderTextId = f.readInt();
                unk2 = f.readInt();
                colorId = f.readInt();
                unk3 = f.readInt();
                unk4 = f.readInt();
                unk5 = f.readInt();
                alignment = (Alignment)f.readShort();
                unk6 = (short)f.readShort();
                unk7 = f.readInt();
                unk8 = f.readInt();
                size = f.readFloat();
                unk9 = f.readInt();
                unk10 = f.readInt();
                unk11 = f.readInt();
                unk12 = f.readInt();
            }

            public void Write(OutputBuffer o)
            {
                o.writeInt((int)TagType.DynamicText);
                o.writeInt(16);

                o.writeInt(characterId);
                o.writeInt(unk1);
                o.writeInt(placeholderTextId);
                o.writeInt(unk2);
                o.writeInt(colorId);
                o.writeInt(unk3);
                o.writeInt(unk4);
                o.writeInt(unk5);
                o.writeShort((short)alignment);
                o.writeShort(unk6);
                o.writeInt(unk7);
                o.writeInt(unk8);
                o.writeFloat(size);
                o.writeInt(unk9);
                o.writeInt(unk10);
                o.writeInt(unk11);
                o.writeInt(unk12);
            }
        }

        public class Sprite
        {
            public class Label
            {
                public int nameId;
                public int startFrame;
                public int unk1;

                public Label() { }

                public Label(InputBuffer f)
                {
                    Read(f);
                }

                public void Read(InputBuffer f)
                {
                    nameId = f.readInt();
                    startFrame = f.readInt();
                    unk1 = f.readInt();
                }

                public void Write(OutputBuffer o)
                {
                    o.writeInt((int)TagType.FrameLabel);
                    o.writeInt(3);
                    o.writeInt(nameId);
                    o.writeInt(startFrame);
                    o.writeInt(unk1);
                }
            }

            public class Placement
            {
                public int characterId;
                public int placementId;
                public int unk1;
                public int nameId;
                public short unk2;
                public short unk3;
                public short depth;
                public short unk4;

                public short unk5;
                public short unk6;
                public ushort positionFlags;
                public short positionId;
                public int colorMultId;
                public int colorAddId;

                public UnhandledTag colorMatrix = null;
                public UnhandledTag unkF014 = null;

                public Placement()
                {
                }

                public Placement(InputBuffer f) : this()
                {
                    Read(f);
                }

                public void Read(InputBuffer f)
                {
                    characterId = f.readInt();
                    placementId = f.readInt();
                    unk1 = f.readInt();
                    nameId = f.readInt();
                    unk2 = f.readShort();
                    unk3 = f.readShort();
                    depth = f.readShort();
                    unk4 = f.readShort();
                    unk5 = f.readShort();
                    unk6 = f.readShort();
                    positionFlags = (ushort)f.readShort();
                    positionId = f.readShort();
                    colorMultId = f.readInt();
                    colorAddId = f.readInt();

                    bool hasColorMatrix = (f.readInt() == 1);
                    bool hasUnkF014 = (f.readInt() == 1);

                    if (hasColorMatrix)
                    {
                        colorMatrix = new UnhandledTag(f);
                    }

                    if (hasUnkF014)
                    {
                        unkF014 = new UnhandledTag(f);
                    }
                }

                public void Write(OutputBuffer o)
                {
                    o.writeInt((int)TagType.PlaceObject);
                    o.writeInt(12);

                    o.writeInt(characterId);
                    o.writeInt(placementId);
                    o.writeInt(unk1);
                    o.writeInt(nameId);
                    o.writeShort(unk2);
                    o.writeShort(unk3);
                    o.writeShort(depth);
                    o.writeShort(unk4);
                    o.writeShort(unk5);
                    o.writeShort(unk6);
                    o.writeShort((short)positionFlags);
                    o.writeShort(positionId);
                    o.writeInt(colorMultId);
                    o.writeInt(colorAddId);

                    o.writeInt((colorMatrix != null) ? 1 : 0);
                    o.writeInt((unkF014 != null) ? 1 : 0);

                    if (colorMatrix != null)
                        colorMatrix.Write(o);

                    if (unkF014 != null)
                        unkF014.Write(o);
                }
            }

            public class Deletion
            {
                public int unk1;
                public short depth;
                public short unk2;

                public Deletion(InputBuffer f)
                {
                    Read(f);
                }

                public void Read(InputBuffer f)
                {
                    unk1 = f.readInt();
                    depth = (short)f.readShort();
                    unk2 = (short)f.readShort();
                }

                public void Write(OutputBuffer o)
                {
                    o.writeInt((int)TagType.RemoveObject);
                    o.writeInt(2);
                    o.writeInt(unk1);
                    o.writeShort(depth);
                    o.writeShort(unk2);
                }
            }

            public class Action
            {
                public int actionId;
                public int unk1;

                public Action(InputBuffer f)
                {
                    Read(f);
                }

                public void Read(InputBuffer f)
                {
                    actionId = f.readInt();
                    unk1 = f.readInt();
                }

                public void Write(OutputBuffer o)
                {
                    o.writeInt((int)TagType.DoAction);
                    o.writeInt(2);
                    o.writeInt(actionId);
                    o.writeInt(unk1);
                }
            }

            public class Frame
            {
                public int id;

                public List<Deletion> deletions = new List<Deletion>();
                public List<Action> actions = new List<Action>();
                public List<Placement> placements = new List<Placement>();

                public Frame()
                {
                }

                public Frame(InputBuffer f) : this()
                {
                    Read(f);
                }

                public void Read(InputBuffer f)
                {
                    id = f.readInt();
                    int numChildren = f.readInt();

                    for (int childId = 0; childId < numChildren; childId++)
                    {
                        TagType childType = (TagType)f.readInt();
                        int childSize = f.readInt();

                        if (childType == TagType.RemoveObject)
                        {
                            deletions.Add(new Deletion(f));
                        }
                        else if (childType == TagType.DoAction)
                        {
                            actions.Add(new Action(f));
                        }
                        else if (childType == TagType.PlaceObject)
                        {
                            placements.Add(new Placement(f));
                        }
                    }
                }

                // NOTE: unlike other tag write functions, this does not include the header
                // so it can be used for both frames and keyframes.
                public void Write(OutputBuffer o)
                {
                    o.writeInt(id);
                    o.writeInt(deletions.Count + actions.Count + placements.Count);

                    foreach (var deletion in deletions)
                    {
                        deletion.Write(o);
                    }

                    foreach (var action in actions)
                    {
                        action.Write(o);
                    }

                    foreach (var placement in placements)
                    {
                        placement.Write(o);
                    }
                }
            }

            public int CharacterId;
            public int unk1;
            public int unk2;
            public int unk3;

            public List<Label> labels = new List<Label>();
            public List<Frame> frames = new List<Frame>();
            public List<Frame> keyframes = new List<Frame>();

            public Sprite()
            {
            }

            public Sprite(InputBuffer f) : this()
            {
                Read(f);
            }

            public void Read(InputBuffer f)
            {
                CharacterId = f.readInt();
                unk1 = f.readInt();
                unk2 = f.readInt();

                int numLabels = f.readInt();
                int numFrames = f.readInt();
                int numKeyframes = f.readInt();

                unk3 = f.readInt();

                for (int i = 0; i < numLabels; i++)
                {
                    f.skip(0x08);

                    labels.Add(new Label(f));
                }

                int totalFrames = numFrames + numKeyframes;
                for (int frameId = 0; frameId < totalFrames; frameId++)
                {
                    TagType frameType = (TagType)f.readInt();
                    f.skip(0x04);

                    Frame frame = new Frame(f);

                    if (frameType == TagType.Keyframe)
                    {
                        keyframes.Add(frame);
                    }
                    else
                    {
                        frames.Add(frame);
                    }
                }
            }

            public void Write(OutputBuffer o)
            {
                o.writeInt((int)TagType.DefineSprite);
                o.writeInt(7);
                o.writeInt(CharacterId);
                o.writeInt(unk1);
                o.writeInt(unk2);
                o.writeInt(labels.Count);
                o.writeInt(frames.Count);
                o.writeInt(keyframes.Count);
                o.writeInt(unk3);

                foreach (var label in labels)
                {
                    label.Write(o);
                }

                foreach (var frame in frames)
                {
                    o.writeInt((int)TagType.ShowFrame);
                    o.writeInt(2);
                    frame.Write(o);
                }

                foreach (var frame in keyframes)
                {
                    o.writeInt((int)TagType.Keyframe);
                    o.writeInt(2);
                    frame.Write(o);
                }
            }
        }

        public class Header
        {
            public int magic;
            public int unk0;
            public int unk1;
            public int unk2;
            public int unk3;
            public int unk4;
            public int unk5;
            public int filesize;
            public int unk6;
            public int unk7;
            public int unk8;
            public int unk9;
            public int unk10;
            public int unk11;
            public int unk12;
            public int unk13;

            public void Write(OutputBuffer o)
            {
                o.writeInt(magic);
                o.writeInt(unk0);
                o.writeInt(unk1);
                o.writeInt(unk2);
                o.writeInt(unk3);
                o.writeInt(unk4);
                o.writeInt(unk5);
                o.writeInt(filesize);
                o.writeInt(unk6);
                o.writeInt(unk7);
                o.writeInt(unk8);
                o.writeInt(unk9);
                o.writeInt(unk10);
                o.writeInt(unk11);
                o.writeInt(unk12);
                o.writeInt(unk13);
            }
        }

        public struct Transform
        {
            public float M11;
            public float M12;
            public float M21;
            public float M22;
            public float M31;
            public float M32;

            public Transform(float[] data)
            {
                M11 = data[0];
                M12 = data[1];
                M21 = data[2];
                M22 = data[3];
                M31 = data[4];
                M32 = data[5];
            }
        }

        public class Vector2
        {
            public float X;
            public float Y;

            public Vector2()
            {
            }

            public Vector2(float x, float y)
            {
                X = x;
                Y = y;
            }

            public override bool Equals(object obj)
            {
                var other = (Vector2)obj;
                return (X == other.X && Y == other.Y);
            }

            public override int GetHashCode()
            {
                return ((int)X ^ (int)Y);
            }
        }

        public Header header = new Header();
        public List<string> symbols = new List<string>();
        public List<Color> colors = new List<Color>();
        public List<Transform> transforms = new List<Transform>();
        public List<Vector2> positions = new List<Vector2>();
        public List<ShapeBounds> bounds = new List<ShapeBounds>();
        public List<TextureAtlas> textureAtlases = new List<TextureAtlas>();
        public List<Shape> shapes = new List<Shape>();
        public List<DynamicText> texts = new List<DynamicText>();
        public List<Sprite> sprites = new List<Sprite>();

        public Properties properties = new Properties();
        public UnhandledTag actionscript;
        public UnhandledTag unkF008;
        public UnhandledTag unkF009;
        public UnhandledTag unkF00A;
        public UnhandledTag unk000A;
        public UnhandledTag unkF00B;
        public Properties2 properties2 = new Properties2();

        public Lumen()
        {
        }

        public Lumen(string filename) : this()
        {
            Read(filename);
        }

        public int AddPosition(Vector2 pos)
        {
            int index = -1;

            if (positions.Contains(pos))
            {
                index = positions.IndexOf(pos);
            }
            else
            {
                index = positions.Count;
                positions.Add(pos);
            }

            return index;
        }

        public int AddString(string str)
        {
            int index = -1;

            if (symbols.Contains(str))
            {
                index = symbols.IndexOf(str);
            }
            else
            {
                index = symbols.Count;
                symbols.Add(str);
            }

            return index;
        }

        public int AddColor(Color color)
        {
            int index = -1;

            if (colors.Contains(color))
            {
                index = colors.IndexOf(color);
            }
            else
            {
                index = colors.Count;
                colors.Add(color);
            }

            return index;
        }

        public int AddTransform(Lumen.Transform xform)
        {
            int index = -1;

            if (transforms.Contains(xform))
            {
                index = transforms.IndexOf(xform);
            }
            else
            {
                index = transforms.Count;
                transforms.Add(xform);
            }

            return index;
        }

        public void Read(string filename)
        {
            InputBuffer f = new InputBuffer(filename);
            header.magic = f.readInt();
            header.unk0 = f.readInt();
            header.unk1 = f.readInt();
            header.unk2 = f.readInt();
            header.unk3 = f.readInt();
            header.unk4 = f.readInt();
            header.unk5 = f.readInt();
            header.filesize = f.readInt();
            header.unk6 = f.readInt();
            header.unk7 = f.readInt();
            header.unk8 = f.readInt();
            header.unk9 = f.readInt();
            header.unk10 = f.readInt();
            header.unk11 = f.readInt();
            header.unk12 = f.readInt();
            header.unk13 = f.readInt();

            bool done = false;
            while (!done)
            {
                uint tagOffset = f.ptr;

                TagType tagType = (TagType)f.readInt();
                int tagSize = f.readInt(); // in dwords!

                switch (tagType)
                {
                    case TagType.Invalid:
                        // uhhh. i think there's a specific exception for this
                        throw new Exception("Malformed file");

                    case TagType.Symbols:
                        int numSymbols = f.readInt();

                        while (symbols.Count < numSymbols)
                        {
                            int len = f.readInt();

                            symbols.Add(f.readString());
                            f.skip(4 - (f.ptr % 4));
                        }

                        break;

                    case TagType.Colors:
                        int numColors = f.readInt();

                        for (int i = 0; i < numColors; i++)
                        {
                            var offs = f.ptr;
                            var color = new Color(f.readShort(), f.readShort(), f.readShort(), f.readShort());
                            AddColor(color);
                        }

                        break;

                    case TagType.Fonts:
                        unk000A = new UnhandledTag(tagType, tagSize, f);
                        break;
                    case TagType.UnkF00A:
                        unkF00A = new UnhandledTag(tagType, tagSize, f);
                        break;
                    case TagType.UnkF00B:
                        unkF00B = new UnhandledTag(tagType, tagSize, f);
                        break;
                    case TagType.UnkF008:
                        unkF008 = new UnhandledTag(tagType, tagSize, f);
                        break;
                    case TagType.UnkF009:
                        unkF009 = new UnhandledTag(tagType, tagSize, f);
                        break;
                    case TagType.Properties2:
                        properties2 = new Properties2(f);
                        break;
                    case TagType.ActionScript:
                        actionscript = new UnhandledTag(tagType, tagSize, f);
                        break;

                    case TagType.End:
                        done = true;
                        break;

                    case TagType.Transforms:
                        int numTransforms = f.readInt();

                        for (int i = 0; i < numTransforms; i++)
                        {
                            var offs = f.ptr;
                            Transform xform = new Transform();
                            xform.M11 = f.readFloat();
                            xform.M12 = f.readFloat();
                            xform.M21 = f.readFloat();
                            xform.M22 = f.readFloat();
                            xform.M31 = f.readFloat();
                            xform.M32 = f.readFloat();

                            var scaleX = Math.Sign(xform.M11) * Math.Sqrt(xform.M11 * xform.M11 + xform.M21 * xform.M21);
                            var scaleY = Math.Sign(xform.M22) * Math.Sqrt(xform.M12 * xform.M12 + xform.M22 * xform.M22);

                            var angleRads = Math.Atan2(xform.M21, xform.M22);

                            transforms.Add(xform);
                        }

                        break;

                    case TagType.Positions:
                        int numPositions = f.readInt();

                        for (int i = 0; i < numPositions; i++)
                        {
                            positions.Add(new Vector2(f.readFloat(), f.readFloat()));
                        }

                        break;

                    case TagType.Bounds:
                        int numBounds = f.readInt();

                        for (int i = 0; i < numBounds; i++)
                        {
                            bounds.Add(new ShapeBounds(f.readFloat(), f.readFloat(), f.readFloat(), f.readFloat()));
                        }
                        break;

                    case TagType.Properties:
                        properties = new Properties(f);
                        break;

                    case TagType.TextureAtlases:
                        int numAtlases = f.readInt();

                        for (int i = 0; i < numAtlases; i++)
                        {
                            TextureAtlas atlas = new TextureAtlas();
                            atlas.id = f.readInt();
                            atlas.nameId = f.readInt();
                            atlas.width = f.readFloat();
                            atlas.height = f.readFloat();

                            textureAtlases.Add(atlas);
                        }

                        break;

                    case TagType.Shape:
                        shapes.Add(new Shape(f));
                        break;

                    case TagType.DynamicText:
                        texts.Add(new DynamicText(f));
                        break;

                    case TagType.DefineSprite:
                        sprites.Add(new Sprite(f));
                        break;

                    default:
                        throw new NotImplementedException($"Unhandled tag id: 0x{(uint)tagType:X} @ 0x{tagOffset:X}");
                }
            }
        } // Read()

        #region serialization
        void writeSymbols(OutputBuffer o)
        {
            OutputBuffer tag = new OutputBuffer();
            tag.writeInt(symbols.Count);

            foreach (var symbol in symbols)
            {
                tag.writeInt(symbol.Length);
                tag.writeString(symbol);

                int padSize = 4 - (tag.Size % 4);
                for (int i = 0; i < padSize; i++)
                {
                    tag.writeByte(0);
                }
            }

            o.writeInt((int)TagType.Symbols);
            o.writeInt(tag.Size / 4);
            o.write(tag);
        }

        void writeColors(OutputBuffer o)
        {
            o.writeInt((int)TagType.Colors);
            o.writeInt(colors.Count * 2 + 1);
            o.writeInt(colors.Count);

            foreach (var color in colors)
            {
                o.writeShort((short)(color.R * 256));
                o.writeShort((short)(color.G * 256));
                o.writeShort((short)(color.B * 256));
                o.writeShort((short)(color.A * 256));
            }
        }

        void writePositions(OutputBuffer o)
        {
            o.writeInt((int)TagType.Positions);
            o.writeInt(positions.Count * 2 + 1);
            o.writeInt(positions.Count);

            foreach (var position in positions)
            {
                o.writeFloat(position.X);
                o.writeFloat(position.Y);
            }
        }

        void writeTransforms(OutputBuffer o)
        {
            o.writeInt((int)TagType.Transforms);
            o.writeInt(transforms.Count * 6 + 1);
            o.writeInt(transforms.Count);

            foreach (var transform in transforms)
            {
                o.writeFloat(transform.M11);
                o.writeFloat(transform.M12);
                o.writeFloat(transform.M21);
                o.writeFloat(transform.M22);
                o.writeFloat(transform.M31);
                o.writeFloat(transform.M32);
            }
        }

        void writeBounds(OutputBuffer o)
        {
            o.writeInt((int)TagType.Bounds);
            o.writeInt(bounds.Count * 4 + 1);
            o.writeInt(bounds.Count);

            foreach (var extent in bounds)
            {
                o.writeFloat(extent.left);
                o.writeFloat(extent.top);
                o.writeFloat(extent.right);
                o.writeFloat(extent.bottom);
            }
        }

        void writeAtlases(OutputBuffer o)
        {
            o.writeInt((int)TagType.TextureAtlases);
            o.writeInt(textureAtlases.Count * 4 + 1);
            o.writeInt(textureAtlases.Count);

            foreach (var atlas in textureAtlases)
            {
                o.writeInt(atlas.id);
                o.writeInt(atlas.nameId);
                o.writeFloat(atlas.width);
                o.writeFloat(atlas.height);
            }
        }

        void writeShapes(OutputBuffer o)
        {
            foreach (var shape in shapes) shape.Write(o);
            {

            }
        }

        void writeSprites(OutputBuffer o)
        {
            foreach (var mc in sprites)
            {
                mc.Write(o);
            }
        }

        void writeTexts(OutputBuffer o)
        {
            foreach (var text in texts)
            {
                text.Write(o);
            }
        }

        #endregion

        public byte[] Rebuild()
        {
            OutputBuffer o = new OutputBuffer();

            // TODO: write correct filesize in header.
            // It isn't checked by the game, but what the hell, right?
            header.Write(o);

            writeSymbols(o);
            writeColors(o);
            writeTransforms(o);
            writePositions(o);
            writeBounds(o);
            if (actionscript == null)
            {
                o.writeInt((int)TagType.ActionScript);
                o.writeInt(1);
                o.writeInt(0);
            }
            else
                actionscript.Write(o);

            writeAtlases(o);

            unkF008.Write(o);
            unkF009.Write(o);
            unkF00A.Write(o);
            unk000A.Write(o);
            unkF00B.Write(o);
            properties.Write(o);

            properties2.numShapes = (uint)shapes.Count;
            properties2.numSprites = (uint)sprites.Count;
            properties2.numTexts = (uint)texts.Count;
            properties2.Write(o);

            writeShapes(o);
            writeSprites(o);
            writeTexts(o);

            o.writeInt((int)TagType.End);
            o.writeInt(0);

            int padSize = (4 - (o.Size % 4)) % 4;
            for (int i = 0; i < padSize; i++)
            {
                o.writeByte(0);
            }

            return o.getBytes();
        }
    }
}
