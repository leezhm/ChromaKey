using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Code4Fun.cn.Kinect.Flickering
{
    class AverageFilter
    {
        /// <summary>
        /// Reset Queue when there are more than AverageFrameCount;
        /// </summary>
        public void ResetQueue(Queue<byte[]> queue, int AverageFrameCount)
        {
            if (AverageFrameCount < queue.Count)
            {
                // Clear
                queue.Dequeue();

                ResetQueue(queue, AverageFrameCount);
            }
        }

        public void Apply(Queue<byte[]> queue, byte[] newPixels, int Afc, int Width, int Height)
        {
            int count = 1;
            Int32[,] sumPixels = new Int32[3, Width * Height];
            double denominator = 0.0f;

            //ResetQueue(queue, Afc);

            foreach (var item in queue)
            {
                double weighting = System.Math.Pow(2.5, System.Math.Exp(count));

                for (int hIndex = 0; hIndex < Height; ++hIndex)
                {
                    for (int wIndex = 0; wIndex < Width; ++wIndex)
                    {
                        int index = wIndex + hIndex * Width;
                        sumPixels[0, index] += (int)(item[index * 4] * weighting);
                        sumPixels[1, index] += (int)(item[index * 4 + 1] * weighting);
                        sumPixels[2, index] += (int)(item[index * 4 + 2] * weighting);
                    }
                }

                denominator += weighting;
                ++count;
            }

            // Average
            for (int hIndex = 0; hIndex < Height; ++hIndex)
            {
                for (int wIndex = 0; wIndex < Width; ++wIndex)
                {
                    int index = wIndex + hIndex * Width;

                    newPixels[index * 4] = (byte)(sumPixels[0, index] / denominator);
                    newPixels[index * 4 + 1] = (byte)(sumPixels[1, index] / denominator);
                    newPixels[index * 4 + 2] = (byte)(sumPixels[2, index] / denominator);
                }
            }
        }
    }
}
