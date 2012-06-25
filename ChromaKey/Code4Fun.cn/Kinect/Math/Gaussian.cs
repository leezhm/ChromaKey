//
//  Gaussian.cs
//
//  Basic context with Gaussian function
//
//  Written by leezhm(at)126.com, 1st June, 2012
//
//  Copyright (c) leezhm(at)126.com. All Right Reserved.
//
//  Last Modified by leezhm(at)126.com on 4th June, 2012.
//

namespace Code4Fun.cn.Kinect.Math
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using System.Threading.Tasks;   // for Parallel

    public class Gaussian
    {
        // Gaussian template
        public static readonly byte[,] Gauss55_273 
            = {{1, 4,  7,  4,  1},
               {4, 16, 26, 16, 4},
               {7, 26, 41, 26, 7},
               {4, 16, 26, 16, 4},
               {1, 4,  7,  4 , 1}};

        public static readonly byte[,] Gauss55_159 
            = {{2, 4,  5,  4,  2},
               {4, 9,  12, 9,  4},
               {5, 12, 15, 12, 5},
               {4, 9,  12, 9,  4},
               {2, 4,  5,  4,  2}};

        /// <summary>
        /// Sigma for Gaussian function
        /// </summary>
        public double Sigma { get; private set; }

        public double Divisor { get; private set; }

        /// <summary>
        /// Size of Gaussian Kernel
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Array of Gaussian Kernel
        /// </summary>
        public int[,] Kernel { get; private set; }

        public Gaussian(int size, double sigma)
        {
            if (0 == (size % 2) || 3 > size || 20 < size)
            {
                throw new ArgumentException(@"Using a unavailable kernel size. 
                                              It is must be odd and between 3 to 20!");
            }

            Size = size;
            Sigma = sigma;

            Kernel = new int[Size, Size];

            // Generate the kernel
            GenerateKernel();
        }

        /// <summary>
        /// Generate the Gaussian Kernel by size and sigma
        /// </summary>
        private void GenerateKernel()
        {
            int num = Size / 2;
            double[,] kernel = new double[Size, Size];
            double sqrSigma = Sigma * Sigma;

            int num2 = -num;
            int num3 = -num;
            for (int i = 0; i < Size; ++i)
            {
                num3 = -num;
                for (int j = 0; j < Size; ++j)
                {
                    kernel[i, j] = Math.Exp(((double)num3 * num3 + (double)num2 * num2) / (-2.0 * sqrSigma)) / 
                                    (MathEx.PI * sqrSigma);

                    ++ num3;
                }
                ++num2;
            }

            double min = kernel[0, 0];

            for (int i = 0; i < Size; ++i)
            {
                for (int j = 0; j < Size; ++j)
                {
                    double item = kernel[i, j] / min;
                    if (ushort.MaxValue < item)
                    {
                        item = ushort.MaxValue;
                    }

                    Kernel[i, j] = (int)item;

                    // count the divisor
                    Divisor += (int)item;
                }
            }
        }

        /// <summary>
        /// Format this class
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string str = "Gaussian Kernel[" + Size + ", " + Sigma + ", " + Divisor + "] = \n";

            for (int i = 0; i < Size; ++i)
            {
                str += "{";
                for (int j = 0; j < Size; ++j)
                {
                    str += Kernel[i, j].ToString() +  (j != (Size - 1)?",\t":"");
                }
                str += "}\n";
            }

            return str; 
        }
    }
}
