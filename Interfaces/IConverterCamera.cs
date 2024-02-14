using AForge.Video.DirectShow;
using camera.Delegates;
using OpenCvSharp;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace camera.Interfaces
{
    internal interface IConverterCamera
    {
        /// <summary>
        /// Инициализирует камеру и таймер для обновления изображения.
        /// </summary>
        /// <param name="videoDevices"></param>
        /// <param name="videoSource"></param>
        /// <param name="timer1"></param>
        /// <param name="videoSource_NewFrame_Delegate_EventHandler"></param>
        /// <param name="Timer_Tick"></param>
        /// <returns></returns>
        public static abstract (FilterInfoCollection, VideoCaptureDevice, DispatcherTimer)
            InitializeCamera(FilterInfoCollection videoDevices, VideoCaptureDevice videoSource,
            DispatcherTimer timer1,
            DelegateUtils.VideoSource_NewFrame_delegate_EventHandler videoSource_NewFrame_Delegate_EventHandler, DelegateUtils.Timer_Tick_delegate_EventHandler Timer_Tick);
        /// <summary>
        /// Преобразует объект BitmapSource в объект OpenCV Mat.
        /// </summary>
        /// <param name="source">Преобразуемый объект BitmapSource.</param>
        /// <returns>Возвращает объект OpenCV Mat, представляющий изображение в формате OpenCV.</returns>
        public static abstract Mat BitmapSourceToMat(BitmapSource source);
        /// <summary>
        /// Преобразует объект OpenCV Mat в объект BitmapImage.
        /// </summary>
        /// <param name="mat">Преобразуемый объект OpenCV Mat.</param>
        /// <returns>Возвращает объект BitmapImage, представляющий изображение в формате WPF.</returns>
        public static abstract BitmapImage MatToBitmapImage(Mat mat);
        /// <summary>
        /// Преобразует объект Bitmap в объект BitmapImage.
        /// </summary>
        /// <param name="bitmap">Преобразуемый объект Bitmap.</param>
        /// <returns>Возвращает объект BitmapImage, представляющий изображение в формате WPF.</returns>
        public static abstract BitmapImage BitmapToImageSource(Bitmap bitmap);
    }
}
