using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections.Concurrent; // for ConcurrentQueue

namespace Code4Fun.cn.Kinect.Flickering
{
    class AverageFilter
    {
        /// <summary>
        /// Reset Queue when there are more than AverageFrameCount;
        /// </summary>
        //public void ResetQueue(ConcurrentQueue<byte[]> queue, int AverageFrameCount)
        //public void ResetQueue(Queue<byte[]> queue, int AverageFrameCount)
        public void ResetLinkedList(LinkedList<byte[]> list, int AverageFrameCount)
        {
            if (AverageFrameCount < list.Count)
            {
                //////// Clear
                ////////byte[] tmp;
                ////////queue.TryDequeue(out tmp);
                //////queue.Dequeue();

                //////ResetQueue(queue, AverageFrameCount);

                // Clear
                list.RemoveFirst();

                ResetLinkedList(list, AverageFrameCount);
            }
        }

        //public void Apply(ConcurrentQueue<byte[]> queue, byte[] newPixels, int Afc, int Width, int Height)
        //public void Apply(Queue<byte[]> queue, byte[] newPixels, int Afc, int Width, int Height)
        public void Apply(LinkedList<byte[]> list, byte[] newPixels, int Afc, int Width, int Height, object gLock)
        {
            int count = 1;
            Int32[,] sumPixels = new Int32[3, Width * Height];
            double denominator = 0.0f;

            //ResetQueue(queue, Afc);
            ResetLinkedList(list, Afc);

            lock (gLock)
            {
                foreach (var item in list)
                {
                    double weighting = System.Math.Pow(1.5, System.Math.Exp(count));

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
