using OpenCvSharp;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace camera.Interfaces
{
    internal interface IConverterCamera
    {
        /// <summary>
        /// Инициализирует камеру и таймер для обновления изображения.
        /// </summary>
        public void InitializeCamera();
        /// <summary>
        /// Преобразует объект BitmapSource в объект OpenCV Mat.
        /// </summary>
        /// <param name="source">Преобразуемый объект BitmapSource.</param>
        /// <returns>Возвращает объект OpenCV Mat, представляющий изображение в формате OpenCV.</returns>
        public Mat BitmapSourceToMat(BitmapSource source);
        /// <summary>
        /// Преобразует объект OpenCV Mat в объект BitmapImage.
        /// </summary>
        /// <param name="mat">Преобразуемый объект OpenCV Mat.</param>
        /// <returns>Возвращает объект BitmapImage, представляющий изображение в формате WPF.</returns>
        public BitmapImage MatToBitmapImage(Mat mat);
        /// <summary>
        /// Преобразует объект Bitmap в объект BitmapImage.
        /// </summary>
        /// <param name="bitmap">Преобразуемый объект Bitmap.</param>
        /// <returns>Возвращает объект BitmapImage, представляющий изображение в формате WPF.</returns>
        public BitmapImage BitmapToImageSource(Bitmap bitmap);
    }
}
