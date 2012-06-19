using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Media;     // for PixelFormat and Color
using System.Threading.Tasks;   // for Parallel

namespace Code4Fun.cn.Kinect.Smoothen
{
    class GenericMedian : BasicSmoothenFilter
    {
        /// <summary>
        /// 
        /// </summary>
        public Color BackgroundColor { get; private set; }

        private int size = 0;
        public int Size
        {
            get {return size;}
            set
            {
                // 3 < size < 15
                size = System.Math.Max(3, System.Math.Min(15, value | 1)); 
            }
        }

        public int WBound { get; private set; }
        public int HBound { get; private set; }

        public GenericMedian(int w, int h, PixelFormat format, Color bgColor, int size)
            : base(w, h, format)
        {
            BackgroundColor = bgColor;
            Size = size;

            WBound = w - 1;
            HBound = h - 1;
        }

        public override void ProcessFilter(byte[] pixels)
        {
            byte[] b = new byte[Size * Size];
            byte[] g = new byte[Size * Size];
            byte[] r = new byte[Size * Size];

            int i = 0;
            int index = 0, newIndex = 0;
            int offSet = Format.BitsPerPixel / 8;
            int radius = Size >> 1, middle = 0;

            for (int hIndex = 0; hIndex < Height; ++hIndex)
            {
                for (int wIndex = 0; wIndex < Width; ++wIndex)
                {
                    index = (wIndex + hIndex * Width) * offSet;
                    if (BackgroundColor.B != pixels[index])
                    {
                        for (int yi = -radius; yi <= radius; ++yi)
                        {
                            for (int xi = -radius; xi <= radius; ++xi)
                            {
                                int xBound = wIndex + xi;
                                int yBound = hIndex + yi;

                                if (0 <= xBound && WBound >= xBound &&
                                    0 <= yBound && HBound >= yBound)
                                {
                                    newIndex = (xBound + yBound * Width) * offSet;

                                    b[i] = pixels[newIndex];
                                    g[i] = pixels[newIndex + 1];
                                    r[i] = pixels[newIndex + 2];
                                }

                                i ++; 
                            }
                        }

                        Array.Sort(b, 0, i);
                        Array.Sort(g, 0, i);
                        Array.Sort(r, 0, i);

                        middle = i >> 1;

                        pixels[index] = b[middle];
                        pixels[index + 1] = g[middle];
                        pixels[index + 2] = r[middle];

                        middle = i = 0;
                    }
                }
            }
        }
    }
}
