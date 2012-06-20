//
//  GaussianFilter.cs
//
//  Gaussian smoothen filter
//
//  Written by leezhm(at)126.com, 4th June, 2012
//
//  Copyright (c) leezhm(at)126.com. All Right Reserved.
//
//  Last Modified by leezhm(at)126.com on 5th June, 2012.
//

namespace Code4Fun.cn.Kinect.Smoothen
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using System.Windows.Media; // for PixelFormat and Color
    using System.Threading.Tasks;   // for Parallel

    public class GaussianFilter : BasicSmoothenFilter
    {
        /// <summary>
        /// The available rectangle of image
        /// </summary>
        public int WBound { get; private set; }
        public int HBound { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int Begin { get; private set; }
        public int End { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public Color BackgroundColor { get; private set; }

        public Code4Fun.cn.Kinect.Math.Gaussian Gauss = null;

        public GaussianFilter(int w, int h, PixelFormat format, Color bgColor)
            : base(w, h, format)
        {
            // Just supported the colorful image and its channel is BGR.
            if (!((PixelFormats.Bgr24 == format) || (PixelFormats.Bgr32 == format) || 
                (PixelFormats.Bgra32 == format)))
            {
                throw new ArgumentException("Unavailable Pixel Format --> {0} ... ", format.ToString());
            }

            WBound = Width - 1;
            HBound = Height - 1;

            BackgroundColor = bgColor;

            // Init Gaussian Kernel
            Gauss = new Kinect.Math.Gaussian(5, 1.02);

            Begin = -((Gauss.Size - 1) / 2);
            End = (Gauss.Size + 1) / 2;
            Offset = (Gauss.Size - 1) / 2;

            Console.WriteLine(Gauss.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pixels"></param>
        public override void ProcessFilter(byte[] pixels)
        {
            int pixelB = 0;
            int pixelG = 0;
            int pixelR = 0;

            int xBound = 0;
            int yBound = 0;

            int index = 0;

            //Parallel.For(0, Height, hIndex =>
                for (int hIndex = 0; hIndex < Height; ++hIndex)
                {
                    for (int wIndex = 0; wIndex < Width; ++wIndex)
                    {
                        index = (wIndex + hIndex * Width) * Format.BitsPerPixel / 8 ;

                        if (!(BackgroundColor.B == pixels[index] &&
                            BackgroundColor.G == pixels[index + 1] &&
                            BackgroundColor.R == pixels[index + 2]))
                        {
                            for (int yi = Begin; yi < End; ++yi)
                            {
                                for (int xi = Begin; xi < End; ++xi)
                                {
                                    xBound = wIndex + xi;
                                    yBound = hIndex + yi;

                                    if (0 <= xBound && WBound >= xBound &&
                                        0 <= yBound && HBound >= yBound)
                                    {
                                        int newIndex = (xBound + yBound * Width) * Format.BitsPerPixel / 8;

                                        pixelB += pixels[newIndex]     * Gauss.Kernel[(xi + Offset), (yi + Offset)];
                                        pixelG += pixels[newIndex + 1] * Gauss.Kernel[(xi + Offset), (yi + Offset)];
                                        pixelR += pixels[newIndex + 2] * Gauss.Kernel[(xi + Offset), (yi + Offset)];
                                    }
                                }
                            }

                            // calculate the average
                            pixels[index]     = (byte)(pixelB / Gauss.Divisor);
                            pixels[index + 1] = (byte)(pixelG / Gauss.Divisor);
                            pixels[index + 2] = (byte)(pixelR / Gauss.Divisor);

                            pixelB = pixelG = pixelR = 0;
                        }
                    }
                }//);
        }
    }
}
