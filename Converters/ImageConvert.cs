using OpenCvSharp.Extensions;
using OpenCvSharp;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using camera.Interfaces;
using AForge.Video.DirectShow;
using System.Windows.Threading;
using camera.Delegates;

namespace camera.Converters
{
    public class ImageConvert:IConverterCamera
    {
        public static Mat BitmapSourceToMat(BitmapSource source)
        {
            if (source != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BitmapEncoder encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(source));
                    encoder.Save(ms);

                    return Cv2.ImDecode(ms.ToArray(), ImreadModes.Color);
                }
            }
            else
            {
                return null;
            }
        }
        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (var memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
        /// <summary>
        /// Метод для инициализации камеры.
        /// </summary>
        /// <param name="videoDevices">Коллекция устройств видеозахвата.</param>
        /// <param name="videoSource">Источник видеопотока.</param>
        /// <param name="timer1">Таймер для обновления изображения.</param>
        /// <param name="videoSource_NewFrame_Delegate_EventHandler">Делегат для обработки нового кадра из видеопотока.</param>
        /// <param name="Timer_Tick">Делегат для обработки события таймера.</param>
        /// <returns>
        /// Кортеж из трех объектов:
        /// - FilterInfoCollection: коллекция доступных устройств видеозахвата.
        /// - VideoCaptureDevice: источник видеопотока.
        /// - DispatcherTimer: таймер для обновления изображения.
        /// </returns>
        public static (FilterInfoCollection, VideoCaptureDevice, DispatcherTimer) 
            InitializeCamera(FilterInfoCollection videoDevices, VideoCaptureDevice videoSource, DispatcherTimer timer1, 
            DelegateUtils.VideoSource_NewFrame_delegate_EventHandler videoSource_NewFrame_Delegate_EventHandler,DelegateUtils.Timer_Tick_delegate_EventHandler Timer_Tick)
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count == 0)
            {
                System.Windows.MessageBox.Show("Камеры не обнаружены");
                return (null,null,null);
            }

            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);

            videoSource.NewFrame += (sender, eventArgs) =>
            {
                videoSource_NewFrame_Delegate_EventHandler(sender, eventArgs);
            };
            // Инициализируем таймер для обновления изображения
            timer1 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer1.Tick += (sender, eventArgs) =>
            {
                Timer_Tick(sender, eventArgs);
            };
            return (videoDevices, videoSource, timer1);
        }
        public static BitmapImage MatToBitmapImage(Mat mat)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Bitmap bitmap = BitmapConverter.ToBitmap(mat);
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
        public static Bitmap BitmapSourceToBitmap(BitmapSource bitmapSource)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(outStream);
                return new Bitmap(outStream);
            }
        }
    }
}
