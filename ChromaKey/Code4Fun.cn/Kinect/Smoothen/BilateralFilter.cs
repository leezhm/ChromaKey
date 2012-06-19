using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Media;     // for PixelFormat and Color
using System.Threading.Tasks;   // for Parallel

namespace Code4Fun.cn.Kinect.Smoothen
{
    class BilateralFilter : BasicSmoothenFilter
    {
        private Code4Fun.cn.Kinect.Math.Bilateral Bi = null;

        public BilateralFilter(int w, int h, PixelFormat format)
            : base(w, h, format)
        {
            Bi = new Math.Bilateral(5, 256, 10, 50);
        }

        public override void ProcessFilter(byte[] pixels)
        {
            double sCorefB = 0;
            double sCorefG = 0;
            double sCorefR = 0;

            double sMembB = 0;
            double sMembG = 0;
            double sMembR = 0;

            double corefB = 0;
            double corefG = 0;
            double corefR = 0;

            int bound = Bi.OffSet;
            int offSet = Format.BitsPerPixel / 8;

            int newIndex = 0;

            for (int hIndex = Bi.OffSet; hIndex < (Height - Bi.OffSet); ++hIndex)
            {
                for (int wIndex = Bi.OffSet; wIndex < (Width - Bi.OffSet); ++wIndex)
                {
                    int index = (wIndex + hIndex * Width) * offSet;

                    sCorefB = sCorefG = sCorefR = 0;
                    sMembB  = sMembG  = sMembR  = 0;
                    corefB  = corefG  = corefR  = 0;

                    for (int i = -Bi.OffSet; i < bound; ++i)
                    {
                        for (int j = -Bi.OffSet; j < bound; ++j)
                        {
                            newIndex = ((wIndex + i) + (hIndex + j) * Width) * offSet;

                            corefB = Bi.SpatialFunc[(i + Bi.OffSet), (j + Bi.OffSet)] * Bi.ColorsFunc[pixels[newIndex], pixels[index]];
                            corefG = Bi.SpatialFunc[(i + Bi.OffSet), (j + Bi.OffSet)] * Bi.ColorsFunc[pixels[newIndex + 1], pixels[index + 1]];
                            corefR = Bi.SpatialFunc[(i + Bi.OffSet), (j + Bi.OffSet)] * Bi.ColorsFunc[pixels[newIndex + 2], pixels[index + 2]];

                            sCorefB += corefB;
                            sCorefG += corefG;
                            sCorefR += corefR;

                            sMembB += corefB * pixels[newIndex];
                            sMembG += corefG * pixels[newIndex + 1];
                            sMembR += corefR * pixels[newIndex + 2];
                        }
                    }

                    pixels[index] = (byte)(sMembB / sCorefB);
                    pixels[index + 1] = (byte)(sMembG / sCorefG);
                    pixels[index + 2] = (byte)(sMembR / sCorefR);
                }
            }
        }
    }
}
