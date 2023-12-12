using AForge.Video.DirectShow;
using AForge.Video;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Drawing;


namespace camera.View
{
    /// <summary>
    /// Логика взаимодействия для CameraViewer.xaml
    /// </summary>
    public partial class CameraViewer : UserControl
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private DispatcherTimer timer;

        public CameraViewer()
        {
            InitializeComponent();
            InitializeCamera();
        }
        private void InitializeCamera()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count == 0)
            {
                MessageBox.Show("Камеры не обнаружены");
                return;
            }

            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoSource.NewFrame += VideoSource_NewFrame;

            // Инициализируем таймер для обновления изображения
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer.Tick += Timer_Tick;
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Обновляем изображение в Image
            Dispatcher.Invoke(() =>
            {
                cameraImage.Source = BitmapToImageSource((Bitmap)eventArgs.Frame.Clone());
            } );
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Запускаем камеру при первом тике таймера
            if (!videoSource.IsRunning)
            {
                videoSource.Start();
            }
        }

        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            // Запускаем таймер при нажатии на кнопку
            timer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Останавливаем камеру при закрытии окна
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
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
