using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;

using Code4Fun.cn.Kinect.Math;
using Code4Fun.cn.Kinect.Smoothen;
using Code4Fun.cn.Kinect.Flickering;

using System.ComponentModel;    // For BackgroundWorker
using System.Threading;         // For DispatcherPriority

using AForge;

namespace ChromaKey
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Using the Nlog file system
        /// </summary>
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 
        /// </summary>
        private KinectSensor gSensor = null;

        private int ColorWidth;
        private int ColorHeight;
        private int DepthWidth;
        private int DepthHeight;

        private ColorImageFormat CIF = ColorImageFormat.RgbResolution640x480Fps30;
        private DepthImageFormat DIF = DepthImageFormat.Resolution640x480Fps30;

        private byte[] ColorPixels = null;
        private WriteableBitmap ColorBitmap = null;

        private byte[] PlayerPixels = null;
        private WriteableBitmap PlayerBitmap = null;

        private short[] DepthDatas = null;
        private ColorImagePoint[] CIP = null;

        private double Divisor = 0.0f;

        private BasicSmoothenFilter smooth = null;
        private BasicSmoothenFilter smooth2 = null;

        private BackgroundWorker globalBWorker = null;

        private bool HadPlayer = false;

        private Queue<byte[]> PixelsQueue = new Queue<byte[]>();
        private AverageFilter Average = new AverageFilter();

        /// <summary>
        /// 
        /// </summary>
        public delegate void HeavyProcessor();

        public MainWindow()
        {
            InitializeComponent();

            // Sensor monitor
            KinectSensor.KinectSensors.StatusChanged += (object sender, StatusChangedEventArgs args) =>
                {
                    if (args.Sensor == gSensor)
                    {
                        if (KinectStatus.Connected != args.Status)
                        {
                            SetKinectSensor(null);
                        }
                    }
                    else if ((null == gSensor) && (KinectStatus.Connected == args.Status))
                    {
                        SetKinectSensor(args.Sensor);
                    }
                };

            foreach (var sensor in KinectSensor.KinectSensors)
            {
                if (KinectStatus.Connected == sensor.Status) 
                {
                    SetKinectSensor(sensor);

                    break;
                }
            }
        }

        private void SetKinectSensor(KinectSensor sensor)
        {
            if (null != gSensor)
            {
                gSensor.Stop();
                gSensor = null;

                logger.Debug("Stop current Kinect Sensor and Set it as null");
            }
            
            if(null != (gSensor = sensor))
            {
                // Enable Kinect Sensor
                gSensor.ColorStream.Enable(CIF);
                gSensor.DepthStream.Enable(DIF);

                // Get the size of Color/Depth Image
                ColorWidth = gSensor.ColorStream.FrameWidth;
                ColorHeight = gSensor.ColorStream.FrameHeight;
                DepthWidth = gSensor.DepthStream.FrameWidth;
                DepthHeight = gSensor.DepthStream.FrameHeight;
                Divisor = ColorWidth / DepthWidth;

                //var parameters = new TransformSmoothParameters
                //{
                //    Smoothing = 0.8f,
                //    Correction = 0.0f,
                //    Prediction = 0.0f,
                //    JitterRadius = 1.0f,
                //    MaxDeviationRadius = 0.5f
                //};

                // If we want to get the player information, we must be enable the Skeleton Stream.
                //gSensor.SkeletonStream.Enable(parameters);
                gSensor.SkeletonStream.Enable();

                // Add the AllFramesReady Event
                gSensor.AllFramesReady += AllFramesReadyEventHandler;

                // Init pixels and bitmap
                if (null == ColorPixels)
                {
                    ColorPixels = new byte[gSensor.ColorStream.FramePixelDataLength];
                }

                if (null == ColorBitmap)
                {
                    ColorBitmap = new WriteableBitmap(ColorWidth, ColorHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                }

                if (null == PlayerPixels)
                {
                    PlayerPixels = new byte[gSensor.DepthStream.FramePixelDataLength * sizeof(int)];
                }

                if (null == PlayerBitmap)
                {
                    PlayerBitmap = new WriteableBitmap(DepthWidth, DepthHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                }

                if (null == DepthDatas)
                {
                    DepthDatas = new short[gSensor.DepthStream.FramePixelDataLength];
                }

                if (null == CIP)
                {
                    CIP = new ColorImagePoint[gSensor.DepthStream.FramePixelDataLength];
                }

                // Init Image Control
                imgColor.Source = ColorBitmap;
                imgPlayer.Source = PlayerBitmap;

                try
                {
                    // Start Kinect Sensor
                    gSensor.Start();
                }
                catch (Exception expt)
                {
                    logger.Fatal(expt.Message);
                    logger.Fatal(expt.StackTrace);
                }
            }
        }

        private void AllFramesReadyEventHandler(object sender, AllFramesReadyEventArgs args)
        {
            using(ColorImageFrame ciFrame = args.OpenColorImageFrame())
            {
                if (null != ciFrame)
                {
                    ciFrame.CopyPixelDataTo(this.ColorPixels);

                    ColorBitmap.WritePixels(new Int32Rect(0, 0, ColorWidth, ColorHeight),
                                            ColorPixels, ColorWidth * sizeof(int), 0); 
                }
            }

            using (DepthImageFrame diFrame = args.OpenDepthImageFrame())
            {
                if (null != diFrame)
                {
                    diFrame.CopyPixelDataTo(this.DepthDatas);
                }
                else
                {
                    return;
                }
            }

            // Clear
            Array.Clear(PlayerPixels, 0, PlayerPixels.Length);
            //System.Threading.Tasks.Parallel.For(0, PlayerPixels.Length, index =>
            //    {
            //        PlayerPixels[index] = 200;
            //    });

            Array.Clear(CIP, 0, CIP.Length);

            gSensor.MapDepthFrameToColorFrame(DIF, DepthDatas, CIF, CIP);

            byte[] pixels = new byte[gSensor.DepthStream.FramePixelDataLength * sizeof(int)];

            // Fill the Player Image
            for (int hIndex = 0; hIndex < DepthHeight; ++hIndex)
            {
                for (int wIndex = 0; wIndex < DepthWidth; ++wIndex)
                {
                    int index = wIndex + hIndex * DepthWidth;
                    //int player = DepthDatas[index] & DepthImageFrame.PlayerIndexBitmask;

                    if (0 < (DepthDatas[index] & DepthImageFrame.PlayerIndexBitmask)) // Just for Player
                    {
                        ColorImagePoint cip = CIP[index];

                        // scale color coordinates to depth resolution
                        int colorInDepthX = (int)(cip.X / this.Divisor);
                        int colorInDepthY = (int)(cip.Y / this.Divisor);

                        if (colorInDepthX > 0 && colorInDepthX < this.DepthWidth &&
                            colorInDepthY >= 0 && colorInDepthY < this.DepthHeight)
                        {
                            // calculate index into the green screen pixel array
                            int playerIndex = (colorInDepthX + (colorInDepthY * this.DepthWidth)) << 2;
                            int colorIndex = (cip.X + cip.Y * ColorWidth) << 2;

                            pixels[playerIndex] = ColorPixels[colorIndex]; //BitConverter.ToInt32(ColorPixels, colorIndex);
                            pixels[playerIndex + 1] = ColorPixels[colorIndex + 1];
                            pixels[playerIndex + 2] = ColorPixels[colorIndex + 2];
                            pixels[playerIndex + 3] = ColorPixels[colorIndex + 3];
                            
                            --playerIndex;
                            --colorIndex;

                            pixels[playerIndex] = ColorPixels[colorIndex]; //BitConverter.ToInt32(ColorPixels, colorIndex);
                            pixels[playerIndex + 1] = ColorPixels[colorIndex + 1];
                            pixels[playerIndex + 2] = ColorPixels[colorIndex + 2];
                            pixels[playerIndex + 3] = ColorPixels[colorIndex + 3];
                        }

                        HadPlayer = true;
                    }
                    //else
                    //{
                    //    HadPlayer = false;
                    //}
                }
            }

            lock (gLock)
            {
                // Enqueue
                Average.ResetQueue(PixelsQueue, 3);
                PixelsQueue.Enqueue(pixels);
            }

            // Smoothen
            if (null == smooth && HadPlayer)
            {
                Color bg = new Color();
                bg.B = bg.G = bg.R = 0;

                // Gaussian
                //smooth = new GaussianFilter(DepthWidth, DepthHeight, PixelFormats.Bgr32, bg);
                
                // Bilateral
                smooth = new BilateralFilter(DepthWidth, DepthHeight, PixelFormats.Bgr32);

                // Median
                smooth2 = new GenericMedian(DepthWidth, DepthHeight, PixelFormats.Bgr32, bg, 3);

                //median = new AForge.Imaging.Filters.Median(5);

                if (null == globalBWorker)
                {
                    globalBWorker = new BackgroundWorker();
                    globalBWorker.DoWork += DoWorking;

                    globalBWorker.RunWorkerAsync();
                }
            }

            ////PlayerBitmap.WritePixels(new Int32Rect(0, 0, DepthWidth, DepthHeight),
            ////    PlayerPixels, DepthWidth * ((PlayerBitmap.Format.BitsPerPixel + 7) / 8), 0);
        }

        private AForge.Imaging.Filters.Median median = null;

        private readonly object gLock = new object();

        private void DoWorking(object sender, DoWorkEventArgs args)
        {
            while (true)
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

                lock (gLock)
                {
                    Average.Apply(PixelsQueue, PlayerPixels, 3, DepthWidth, DepthHeight);
                }

                //smooth.ProcessFilter(PlayerPixels);
                //smooth2.ProcessFilter(PlayerPixels);

                //median.Apply(

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    PlayerBitmap.WritePixels(new Int32Rect(0, 0, DepthWidth, DepthHeight),
                                    PlayerPixels, DepthWidth * ((PlayerBitmap.Format.BitsPerPixel + 7) / 8), 0);
                });
                sw.Stop();

                Console.WriteLine("Time -> " + sw.ElapsedMilliseconds);

                System.Threading.Thread.Sleep(1);
            }
        }
    }
}
