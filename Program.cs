using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace swf2lm
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new MainForm());

            var inputFilename = @"C:\s4explore\workspace\content\patch\data\ui\lumen\main\main.swf";

            if (args.Length > 0)
                inputFilename = args[0];

            var outputPath = Path.GetDirectoryName(inputFilename);
            var outputFilename = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(inputFilename) + ".lm");

            var swf = new SWF(inputFilename);
            var lm = new Lumen();
            var textures = new List<Image>();

            lm.AddColor(new Lumen.Color(0xFFFFFFFF));
            lm.AddColor(new Lumen.Color(0x00000000));

            lm.symbols.Add("");

            lm.properties = new Lumen.Properties();
            lm.properties.unk0 = 0;
            lm.properties.unk1 = (uint)lm.AddString("lmf"); // probably correct
            lm.properties.unk2 = (uint)lm.AddString("15");
            lm.properties.unk4 = -1;
            lm.properties.unk7 = 0;
            lm.properties.framerate = swf.Framerate;
            lm.properties.width = swf.rect.maxX;
            lm.properties.height = swf.rect.maxY;
            lm.properties.unk8 = 0;
            lm.properties.unk9 = 0;

            var currentFrameId = 0;
            var currentFrame = new Lumen.Sprite.Frame();
            var currentCharacterId = 0;
            Lumen.Sprite currentSprite;

            var rootMc = new Lumen.Sprite();
            currentSprite = rootMc;
            rootMc.CharacterId = ++currentCharacterId;
            rootMc.unk1 = 0;
            rootMc.unk2 = 0;
            rootMc.unk3 = 0x10000;
            lm.sprites.Add(rootMc);

            foreach (var tag in swf.Tags)
            {
                if (tag is SWF.DefineBitsJPEG3)
                {
                    var ttag = (SWF.DefineBitsJPEG3)tag;
                    textures.Add(ttag.Image);
                }
                else if (tag is SWF.DefineSprite)
                {
                    var ttag = (SWF.DefineSprite)tag;
                    currentSprite.CharacterId = ttag.CharacterId;

                    if (currentSprite.CharacterId > currentCharacterId)
                        currentCharacterId = currentSprite.CharacterId;
                }
                else if (tag is SWF.FrameLabel)
                {
                    var ttag = (SWF.FrameLabel)tag;
                    var label = new Lumen.Sprite.Label();
                    label.nameId = lm.AddString(ttag.name);
                    label.startFrame = currentFrameId;
                    currentSprite.labels.Add(label);
                }
                else if (tag is SWF.ShowFrame)
                {
                    currentFrameId++;
                    currentFrame = new Lumen.Sprite.Frame();
                }
                else if (tag is SWF.End)
                {
                    
                }
                else if (tag is SWF.DefineBitsLossless)
                {
                    var ttag = (SWF.DefineBitsLossless)tag;

                    var shape = new Lumen.Shape();
                    shape.characterId = ++currentCharacterId;
                    var img = ttag.Image;
                    textures.Add(img);

                    var graphic = new Lumen.Graphic();
                    graphic.atlasId = 0; // FIXME
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
                else if (tag is SWF.DefineShape)
                {
                    
                }
                else if (tag is SWF.PlaceObject2)
                {
                    var ttag = (SWF.PlaceObject2)tag;

                    var placement = new Lumen.Sprite.Placement();
                    if (ttag.characterId == -1)
                    {
                        placement.characterId = 0;
                        placement.placementId = -1;
                        placement.unk2 = 2;
                    }
                    else
                    {
                        placement.characterId = ttag.characterId;
                        placement.placementId = 0;
                        placement.unk2 = 1;
                    }

                    if (ttag.depth > lm.properties.maxDepth)
                        lm.properties.maxDepth = (ushort)ttag.depth;

                    placement.depth = ttag.depth;
                    placement.unk1 = 0;
                    placement.nameId = 0; // FIXME
                    placement.unk3 = 0;
                    placement.unk4 = 0;
                    placement.unk5 = 0;
                    placement.unk6 = 0;

                    if (ttag.colorXform != null)
                    {
                        placement.colorMultId = lm.AddColor(ttag.colorXform.Mult);
                        placement.colorAddId = lm.AddColor(ttag.colorXform.Add);
                    }
                    else
                    {
                        placement.colorMultId = 0;
                        placement.colorAddId = 1;
                    }

                    // use position entry if no scale or rotation
                    if (!ttag.matrix.hasRotation && !ttag.matrix.hasScale)
                    {
                        placement.positionFlags = 0x8000;
                        placement.positionId = (short)lm.AddPosition(new Lumen.Vector2(ttag.matrix.data[4], ttag.matrix.data[5]));
                    }
                    else
                    {
                        placement.positionFlags = 0;
                        placement.positionId = (short)lm.AddTransform(new Lumen.Transform(ttag.matrix.data));
                    }

                    var frame = new Lumen.Sprite.Frame();
                    frame.id = currentSprite.frames.Count;
                    frame.placements.Add(placement);
                    currentSprite.frames.Add(frame);
                    currentFrame = frame;
                }
                else
                {

                }
            }

            lm.properties.maxCharacterId = lm.properties.maxCharacterId2 = (uint)currentCharacterId;

            foreach (var sprite in lm.sprites)
            {
                int i = 0;
                foreach (var label in sprite.labels)
                {
                    var keyframe = new Lumen.Sprite.Frame();
                    keyframe.placements = sprite.frames[label.startFrame].placements;
                    keyframe.deletions = sprite.frames[label.startFrame].deletions;
                    keyframe.actions = sprite.frames[label.startFrame].actions;
                    keyframe.id = i++;
                    sprite.keyframes.Add(keyframe);
                }
            }

            //var packer = new TexturePacker();
            //packer.Process(textures, outputPath);

            lm.unkF008 = new Lumen.UnhandledTag(Lumen.TagType.UnkF008, 1, new byte[] { 0, 0, 0, 0 });
            lm.unkF009 = new Lumen.UnhandledTag(Lumen.TagType.UnkF009, 1, new byte[] { 0, 0, 0, 0 });
            lm.unkF00A = new Lumen.UnhandledTag(Lumen.TagType.UnkF00A, 1, new byte[] { 0, 0, 0, 0 });
            lm.unk000A = new Lumen.UnhandledTag(Lumen.TagType.Fonts, 1, new byte[] { 0, 0, 0, 0 });
            lm.unkF00B = new Lumen.UnhandledTag(Lumen.TagType.UnkF00B, 1, new byte[] { 0, 0, 0, 1 });

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

            var atlas = new Lumen.TextureAtlas();
            atlas.id = lm.textureAtlases.Count;
            atlas.nameId = lm.AddString($"{atlas.id:d5}");
            atlas.width = 512;
            atlas.height = 512;
            lm.textureAtlases.Add(atlas);

            using (var fs = new FileStream(outputFilename, FileMode.Create))
            {
                var lmData = lm.Rebuild();
                lm.header.filesize = lmData.Length;
                lmData = lm.Rebuild();
                fs.Write(lmData, 0, lmData.Length);
            }

            //var reLm = new Lumen(@"C:\s4explore\workspace\content\patch\data\ui\lumen\main\main.lm");
            //using (var fs = new FileStream(@"C:\s4explore\workspace\content\patch\data\ui\lumen\main\main.lm", FileMode.Create))
            //{
            //    var lmData = reLm.Rebuild();
            //    reLm.header.filesize = lmData.Length;
            //    lmData = reLm.Rebuild();
            //    fs.Write(lmData, 0, lmData.Length);
            //}
        }
    }
}
