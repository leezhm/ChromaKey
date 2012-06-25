using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Code4Fun.cn.Kinect.Math
{
    class Bilateral
    {
        public int Size { get; private set; }

        public int ColorCount { get; private set; }

        public int OffSet { get; private set; }

        public double SpatialFactor { get; private set; }
        public double ColorsFactor { get; private set; }

        public double[,] SpatialFunc { get; private set; }
        public double[,] ColorsFunc { get; private set; }

        public Bilateral(int size, int count, double sFactor, double cFactor)
        {
            Size = size;
            ColorCount = count;
            OffSet = Size / 2;

            SpatialFactor = sFactor;
            ColorsFactor = cFactor;

            SpatialFunc = new double[Size, Size];
            ColorsFunc = new double[ColorCount, ColorCount];

            // Init
            InitSpatialFunc();
            InitColorsFunc();
        }

        private void InitSpatialFunc()
        {
            int kernelRadius = Size / 2;

            for (int i = 0; i < Size; ++i)
            {
                int ti1 = (i - kernelRadius) * (i - kernelRadius);

                for (int j = 0; j < Size; ++j)
                {
                    int ti2 = (j - kernelRadius) * (j - kernelRadius);

                    SpatialFunc[i, j] = System.Math.Exp(-0.5 * System.Math.Pow((System.Math.Sqrt(ti1 + ti2) / SpatialFactor), 2));
                    //= Math.Exp(-0.5 * Math.Pow(Math.Sqrt((ti2 + tk2) / spatialFactor), 2));
                }
            }
        }

        private void InitColorsFunc()
        {
            for (int i = 0; i < ColorCount; ++i)
            {
                for (int j = 0; j < ColorCount; ++j)
                {
                    ColorsFunc[i, j] = System.Math.Exp(-0.5 * (System.Math.Pow(System.Math.Abs(i - j) / ColorsFactor, 2)));
                }
            }
        }
    }
}
