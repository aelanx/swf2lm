using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Drawing.Imaging;
using ImageMagick;

namespace swf2lm
{
    public class TextureInfo
    {
        public int Width;
        public int Height;
        public Image Image;
    }

    public enum SplitType
    {
        Horizontal,
        Vertical,
    }

    public class Node
    {
        public Rectangle Bounds;
        public TextureInfo Texture;
        public SplitType SplitType;
    }

    public class Atlas
    {
        public int Width;
        public int Height;
        public List<Node> Nodes;
    }

    public class TexturePacker
    {
        public int Padding;
        public int AtlasSize;
        public List<Atlas> Atlases;

        // http://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
        int nextPowerOfTwo(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        public void Process(List<Image> textureImages)
        {
            Padding = 1;
            AtlasSize = 2048;

            var textures = new List<TextureInfo>();
            foreach (var img in textureImages)
            {
                if (img == null)
                    continue;

                var ti = new TextureInfo();

                if (img.Width > AtlasSize)
                    AtlasSize = nextPowerOfTwo(img.Width);
                if (img.Height > AtlasSize)
                    AtlasSize = nextPowerOfTwo(img.Height);

                ti.Width = img.Width;
                ti.Height = img.Height;
                ti.Image = img;

                textures.Add(ti);
            }

            //2: generate as many atlases as needed (with the latest one as small as possible)
            Atlases = new List<Atlas>();
            while (textures.Count > 0)
            {
                var atlas = new Atlas();
                atlas.Width = AtlasSize;
                atlas.Height = AtlasSize;

                List<TextureInfo> leftovers = LayoutAtlas(textures, atlas);

                if (leftovers.Count == 0)
                {
                    // we reached the last atlas. Check if this last atlas could have been twice smaller
                    while (leftovers.Count == 0)
                    {
                        atlas.Width /= 2;
                        atlas.Height /= 2;
                        leftovers = LayoutAtlas(textures, atlas);
                    }
                    // we need to go 1 step larger as we found the first size that is to small
                    atlas.Width *= 2;
                    atlas.Height *= 2;
                    leftovers = LayoutAtlas(textures, atlas);
                }

                Atlases.Add(atlas);

                textures = leftovers;
            }

            //
            int atlasId = 0;
            foreach (Atlas atlas in Atlases)
            {
                var img = new Bitmap(atlas.Width, atlas.Height, PixelFormat.Format32bppArgb);
                var g = Graphics.FromImage(img);

                foreach (Node node in atlas.Nodes)
                {
                    if (node.Texture == null)
                        continue;
                    g.DrawImage(node.Texture.Image, node.Bounds);
                }

                var s = new MagickImage(img);
                s.Format = MagickFormat.Dxt5;

                var nut = new Nut();
                nut.format = GTX.TextureFormat.DXT5;
                nut.id = atlasId;
                nut.data = s.ToByteArray();
                nut.width = img.Width;
                nut.height = img.Height;

                var filename = $@"C:\s4explore\workspace\content\patch\data\ui\lumen\main\img-{atlasId:D5}.nut";
                using (var fs = new FileStream(filename, FileMode.Create))
                {
                    var nutData = nut.Rebuild();
                    fs.Write(nutData, 0, nutData.Length);
                }

                atlasId++;
            }
        }

        private void HorizontalSplit(Node _ToSplit, int _Width, int _Height, List<Node> _List)
        {
            Node n1 = new Node();
            n1.Bounds.X = _ToSplit.Bounds.X + _Width + Padding;
            n1.Bounds.Y = _ToSplit.Bounds.Y;
            n1.Bounds.Width = _ToSplit.Bounds.Width - _Width - Padding;
            n1.Bounds.Height = _Height;
            n1.SplitType = SplitType.Vertical;

            Node n2 = new Node();
            n2.Bounds.X = _ToSplit.Bounds.X;
            n2.Bounds.Y = _ToSplit.Bounds.Y + _Height + Padding;
            n2.Bounds.Width = _ToSplit.Bounds.Width;
            n2.Bounds.Height = _ToSplit.Bounds.Height - _Height - Padding;
            n2.SplitType = SplitType.Horizontal;

            if (n1.Bounds.Width > 0 && n1.Bounds.Height > 0)
                _List.Add(n1);
            if (n2.Bounds.Width > 0 && n2.Bounds.Height > 0)
                _List.Add(n2);
        }

        private void VerticalSplit(Node _ToSplit, int _Width, int _Height, List<Node> _List)
        {
            Node n1 = new Node();
            n1.Bounds.X = _ToSplit.Bounds.X + _Width + Padding;
            n1.Bounds.Y = _ToSplit.Bounds.Y;
            n1.Bounds.Width = _ToSplit.Bounds.Width - _Width - Padding;
            n1.Bounds.Height = _ToSplit.Bounds.Height;
            n1.SplitType = SplitType.Vertical;

            Node n2 = new Node();
            n2.Bounds.X = _ToSplit.Bounds.X;
            n2.Bounds.Y = _ToSplit.Bounds.Y + _Height + Padding;
            n2.Bounds.Width = _Width;
            n2.Bounds.Height = _ToSplit.Bounds.Height - _Height - Padding;
            n2.SplitType = SplitType.Horizontal;

            if (n1.Bounds.Width > 0 && n1.Bounds.Height > 0)
                _List.Add(n1);
            if (n2.Bounds.Width > 0 && n2.Bounds.Height > 0)
                _List.Add(n2);
        }

        private TextureInfo FindBestFitForNode(Node _Node, List<TextureInfo> _Textures)
        {
            TextureInfo bestFit = null;

            float nodeArea = _Node.Bounds.Width * _Node.Bounds.Height;
            float maxCriteria = 0.0f;

            foreach (TextureInfo ti in _Textures)
            {
                if (ti.Width <= _Node.Bounds.Width && ti.Height <= _Node.Bounds.Height)
                {
                    float textureArea = ti.Width * ti.Height;
                    float coverage = textureArea / nodeArea;
                    if (coverage > maxCriteria)
                    {
                        maxCriteria = coverage;
                        bestFit = ti;
                    }
                }
            }

            return bestFit;
        }

        private List<TextureInfo> LayoutAtlas(List<TextureInfo> textures_, Atlas atlas)
        {
            List<Node> freeList = new List<Node>();
            List<TextureInfo> textures = textures_.ToList();

            atlas.Nodes = new List<Node>();

            var root = new Node();
            root.Bounds.Size = new Size(atlas.Width, atlas.Height);
            root.SplitType = SplitType.Horizontal;
            freeList.Add(root);

            while (freeList.Count > 0 && textures.Count > 0)
            {
                Node node = freeList[0];
                freeList.RemoveAt(0);

                TextureInfo bestFit = FindBestFitForNode(node, textures);
                if (bestFit != null)
                {
                    if (node.SplitType == SplitType.Horizontal)
                        HorizontalSplit(node, bestFit.Width, bestFit.Height, freeList);
                    else
                        VerticalSplit(node, bestFit.Width, bestFit.Height, freeList);

                    node.Texture = bestFit;
                    node.Bounds.Width = bestFit.Width;
                    node.Bounds.Height = bestFit.Height;

                    textures.Remove(bestFit);
                }

                atlas.Nodes.Add(node);
            }

            return textures;
        }
    }
}