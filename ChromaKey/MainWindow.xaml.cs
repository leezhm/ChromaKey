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

using System.ComponentModel;    // For BackgroundWorker
using System.Threading;         // For DispatcherPriority

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

        private Size ColorSize;
        private Size DepthSize;

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
                ColorSize = new Size(gSensor.ColorStream.FrameWidth, gSensor.ColorStream.FrameHeight);
                DepthSize = new Size(gSensor.DepthStream.FrameWidth, gSensor.DepthStream.FrameHeight);
                Divisor = ColorSize.Width / DepthSize.Width;

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
                    ColorBitmap = new WriteableBitmap((int)ColorSize.Width, (int)ColorSize.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                }

                if (null == PlayerPixels)
                {
                    PlayerPixels = new byte[gSensor.DepthStream.FramePixelDataLength * sizeof(int)];
                }

                if (null == PlayerBitmap)
                {
                    PlayerBitmap = new WriteableBitmap((int)DepthSize.Width, (int)DepthSize.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
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

                    ColorBitmap.WritePixels(new Int32Rect(0, 0, (int)ColorSize.Width, (int)ColorSize.Height),
                                            ColorPixels, (int)ColorSize.Width * sizeof(int), 0); 
                }
            }

            using (DepthImageFrame diFrame = args.OpenDepthImageFrame())
            {
                if (null != diFrame)
                {
                    diFrame.CopyPixelDataTo(this.DepthDatas);
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

            // Fill the Player Image
            for (int hIndex = 0; hIndex < DepthSize.Height; ++hIndex)
            {
                for (int wIndex = 0; wIndex < DepthSize.Width; ++wIndex)
                {
                    int index = wIndex + hIndex * (int)DepthSize.Width;
                    short player = (short)(DepthDatas[index] & DepthImageFrame.PlayerIndexBitmask);

                    if (0 < player) // Just for Player
                    {
                        ColorImagePoint cip = CIP[index];

                        // scale color coordinates to depth resolution
                        int colorInDepthX = (int)(cip.X / this.Divisor);
                        int colorInDepthY = (int)(cip.Y / this.Divisor);

                        if (colorInDepthX > 0 && colorInDepthX < this.DepthSize.Width &&
                            colorInDepthY >= 0 && colorInDepthY < this.DepthSize.Height)
                        {
                            // calculate index into the green screen pixel array
                            int playerIndex = (colorInDepthX + (colorInDepthY * (int)this.DepthSize.Width)) * sizeof(int);
                            int colorIndex = (cip.X + cip.Y * (int)ColorSize.Width) * sizeof(int);

                            PlayerPixels[playerIndex] = ColorPixels[colorIndex]; //BitConverter.ToInt32(ColorPixels, colorIndex);
                            PlayerPixels[playerIndex + 1] = ColorPixels[colorIndex + 1];
                            PlayerPixels[playerIndex + 2] = ColorPixels[colorIndex + 2];
                            PlayerPixels[playerIndex + 3] = ColorPixels[colorIndex + 3];

                            --playerIndex;
                            --colorIndex;

                            PlayerPixels[playerIndex] = ColorPixels[colorIndex]; //BitConverter.ToInt32(ColorPixels, colorIndex);
                            PlayerPixels[playerIndex + 1] = ColorPixels[colorIndex + 1];
                            PlayerPixels[playerIndex + 2] = ColorPixels[colorIndex + 2];
                            PlayerPixels[playerIndex + 3] = ColorPixels[colorIndex + 3];
                        }

                        HadPlayer = true;
                    }
                    //else
                    //{
                    //    HadPlayer = false;
                    //}
                }
            }

            // Smoothen
            if (null == smooth && HadPlayer)
            {
                Color bg = new Color();
                bg.B = 0;
                bg.G = 0;
                bg.R = 0;

                // Gaussian
                smooth = new GaussianFilter((int)DepthSize.Width, (int)DepthSize.Height, PixelFormats.Bgr32, bg);
                
                // Bilateral
                //smooth = new BilateralFilter((int)DepthSize.Width, (int)DepthSize.Height, PixelFormats.Bgr32);

                // Median
                smooth2 = new GenericMedian((int)DepthSize.Width, (int)DepthSize.Height, PixelFormats.Bgr32, bg, 7);

                if (null == globalBWorker)
                {
                    globalBWorker = new BackgroundWorker();
                    globalBWorker.DoWork += DoWorking;

                    globalBWorker.RunWorkerAsync();
                }
            }
        }

        private void DoWorking(object sender, DoWorkEventArgs args)
        {
            while (true)
            {
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                smooth.ProcessFilter(PlayerPixels);
                //smooth2.ProcessFilter(PlayerPixels);

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    PlayerBitmap.WritePixels(new Int32Rect(0, 0, (int)DepthSize.Width, (int)DepthSize.Height),
                                    PlayerPixels, (int)DepthSize.Width * ((PlayerBitmap.Format.BitsPerPixel + 7) / 8), 0);
                });
                sw.Stop();

                Console.WriteLine("Time -> " + sw.ElapsedMilliseconds);

                System.Threading.Thread.Sleep(1);
            }
        }
    }
}
