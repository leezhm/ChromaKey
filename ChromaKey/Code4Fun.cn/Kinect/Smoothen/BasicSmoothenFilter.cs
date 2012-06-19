//
//  BasicSmoothenFilter.cs
//
//  Basic smoothen filter
//
//  Written by leezhm(at)126.com, 4th June, 2012
//
//  Copyright (c) leezhm(at)126.com. All Right Reserved.
//
//  Last Modified by leezhm(at)126.com on 4th June, 2012.
//

namespace Code4Fun.cn.Kinect.Smoothen
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using System.Windows.Media; // for PixelFormats

    public abstract class BasicSmoothenFilter
    {
        /// <summary>
        /// Width and Height
        /// </summary>
        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>
        /// The format of current image
        /// </summary>
        public PixelFormat Format { get; private set; }

        public BasicSmoothenFilter(int w, int h, PixelFormat format)
        {
            if (0 != w && 0 != h)
            {
                Width = w;
                Height = h;
            }
            else
            {
                throw new ArgumentException("The size of image can not be zero ...");
            }

            Format = format;
        }

        public abstract void ProcessFilter(byte[] pixels);
    }
}
