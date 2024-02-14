using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;


namespace camera.View
{
    /// <summary>
    /// Interaction logic for RotateImage.xaml
    /// </summary>
    public partial class RotateImage : UserControl
    {
        private VideoCapture videoCapture;
        public DispatcherTimer timer2;

        public RotateImage()
        {
            InitializeComponent();
            InitializeCamera();
        }

        private void InitializeCamera()
        {
            // Initialize the camera
            videoCapture = new VideoCapture(1); // 0 represents the default camera, change it if needed

            // Check if the camera is opened successfully
            if (!videoCapture.IsOpened)
            {
                MessageBox.Show("Camera not found.");
                return;
            }

            // Initialize the timer for updating the image
            timer2 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer2.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Получаем кадр с камеры
                Mat frame = new Mat();
                videoCapture.Read(frame);

                // Проверяем, что кадр действителен
                if (frame.IsEmpty)
                    return;

                Bitmap bitmap = ConvertMatToBitmap(frame);
                BitmapSource bitmapSource = ConvertBitmapToBitmapImage(bitmap);

                // Отображаем кадр в элементе управления Image с именем "imageControl"
                imageControl.Source = bitmapSource;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error: {ex.Message}");
                MessageBox.Show(ex.Message);
            }
        }

        private Bitmap ConvertMatToBitmap(Mat mat)
        {
            // Преобразование Mat в Image<Bgr, byte> и затем в Bitmap
            using (var image = mat.ToImage<Bgr, byte>())
            {
                return ImageToBitmap(image);
            }
        }

        private Bitmap ImageToBitmap(Image<Bgr, byte> image)
        {
            var bitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmap.PixelFormat);

            try
            {
                // Получаем указатель на данные изображения
                IntPtr ptr = bitmapData.Scan0;

                // Копируем данные из Image<Bgr, byte> в Bitmap
                System.Runtime.InteropServices.Marshal.Copy(image.Bytes, 0, ptr, image.Bytes.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        private BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
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

    }
}
