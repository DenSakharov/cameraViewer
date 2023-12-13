using AForge.Video.DirectShow;
using AForge.Video;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Windows.Media;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;
using System.Text;

namespace camera.View
{
    /// <summary>
    /// Логика взаимодействия для CameraViewer.xaml
    /// </summary>
    public partial class CameraViewer : UserControl
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private DispatcherTimer timer1;

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

            //videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoSource.NewFrame += VideoSource_NewFrame;

            // Инициализируем таймер для обновления изображения
            timer1 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer1.Tick += Timer_Tick;
        }
        private Mat BitmapSourceToMat(BitmapSource source)
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
        private BitmapImage MatToBitmapImage(Mat mat)
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
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Обновляем изображение в Image
            if (stop)
            {
                ///участок код для контуров
                Mat matFrame = BitmapSourceToMat(BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
                Cv2.CvtColor(matFrame, matFrame, ColorConversionCodes.BGR2GRAY);
                Cv2.Canny(matFrame, matFrame, 50, 150);

                ///

                /*
                Mat matFrame1 = BitmapSourceToMat(BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
                Cv2.CvtColor(matFrame1, matFrame1, ColorConversionCodes.BGR2GRAY);
                Cv2.Canny(matFrame1, matFrame1, 50, 150);

                double angle = 45.0;

                var center = new Point2f(matFrame1.Width / 2.0f, matFrame1.Height / 2.0f);

                // Матрица аффинного преобразования (вращение вокруг оси Z)
                Mat rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);

                // Применяем аффинное преобразование
                Cv2.WarpAffine(matFrame1, matFrame1, rotationMatrix, matFrame1.Size());

                // Матрица аффинного преобразования для изменения масштаба (приближения и отдаления)
                var scaleMatrix = new Mat(2, 3, MatType.CV_64F, new double[] { 1.0, 0.0, 0.0, 0.0, 0.5, 0.0 }); // Уменьшение верхней части изображения в 2 раза

                // Применяем второе аффинное преобразование
                Cv2.WarpAffine(matFrame1, matFrame1, scaleMatrix, matFrame1.Size());
                */
                ///
                Dispatcher.Invoke(() =>
                {
                    ///обычная камера
                    cameraImage00.Source = BitmapToImageSource((Bitmap)eventArgs.Frame.Clone());

                    ///выделение контуров
                    BitmapImage bitmapImage = MatToBitmapImage(matFrame);
                    cameraImage01.Source = bitmapImage;

                    ///
                    //BitmapImage bitmapImage1 = MatToBitmapImage(matFrame1);
                    //cameraImage02.Source = bitmapImage1;
                });

                #region Dispatcher 02
                //                Dispatcher.Invoke(() =>
                //                {
                //                    // Преобразование BitmapSource в Mat
                //                    Mat matFrame1 = BitmapSourceToMat(BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));

                //                    // Настройка параметров калибровки (замените на ваши реальные значения)
                //                    Size patternSize = new Size(8, 6); // размер шахматной доски
                //                    float squareSize = 1.0f; // размер квадрата на шахматной доске в метрах

                //                    // Подготовка массивов для хранения углов и 3D координат шахматных углов
                //                    List<Point3f> objectPoints = new List<Point3f>();
                //                    List<Point2f[]> cornersList = new List<Point2f[]>();

                //                    // Заполнение objectPoints значениями для шахматной доски
                //                    for (int i = 0; i < patternSize.Height; i++)
                //                    {
                //                        for (int j = 0; j < patternSize.Width; j++)
                //                        {
                //                            objectPoints.Add(new Point3f(j * squareSize, i * squareSize, 0.0f));
                //                        }
                //                    }

                //                    Mat grayMat = new Mat();
                //                    Cv2.CvtColor(matFrame1, grayMat, ColorConversionCodes.BGR2GRAY);

                //                    bool foundCorners = Cv2.FindChessboardCorners(grayMat, patternSize, out Point2f[] corners);

                //                    if (foundCorners)
                //                    {
                //                        // Рисуем углы на изображении (для визуализации)
                //                        Cv2.DrawChessboardCorners(matFrame1, patternSize, corners, foundCorners);
                //                        BitmapSource undistortedBitmapSource1 = MatToBitmapImage(matFrame1);
                //                        cameraImage02.Source = undistortedBitmapSource1;



                //                        // Преобразование массива Point2f в Mat
                //                        Mat srcPointsMat = new Mat(corners.Length, 1, MatType.CV_32FC2, corners);

                //                        // Получение массива Point2f для верхнего вида
                //                        Point2f[] topViewPoints = GetTopViewPoints(patternSize);

                //                        // Проверка размерности массивов точек
                //                        if (topViewPoints.Length != corners.Length)
                //                        {
                //                            // Обработка ошибки, например, вывод сообщения
                //                            Console.WriteLine("Ошибка: Неверные размеры массивов точек для гомографии.");
                //                            return;
                //                        }

                //                        // Преобразование массива Point2f в Mat
                //                        Mat dstPointsMat = new Mat(topViewPoints.Length, 1, MatType.CV_32FC2, topViewPoints);

                //                        // Получение матрицы гомографии
                //                        Mat homography = Cv2.FindHomography(srcPointsMat, dstPointsMat, HomographyMethods.Ransac);


                //                        // Применение гомографии к изображению
                //                        Mat topViewImage = new Mat();
                //                        Cv2.WarpPerspective(matFrame1, topViewImage, homography, new Size(undistortedBitmapSource1.Width, undistortedBitmapSource1.Height));

                //                        // Преобразование Mat в BitmapSource и отображение
                //                        BitmapSource topViewBitmapSource = MatToBitmapImage(topViewImage);
                //                        cameraImage02.Source = topViewBitmapSource;

                //                        // Функция для получения координат вершин шахматной доски для гомографии
                //                        Point2f[] GetTopViewPoints(Size patternSize)
                //                        {
                //                            // Размер шахматной доски (в клетках)
                //                            int width = (int)patternSize.Width - 1;
                //                            int height = (int)patternSize.Height - 1;

                //                            // Координаты вершин шахматной доски
                //                            Point2f[] points =
                //                            {
                //        new Point2f(0, 0),
                //        new Point2f(width, 0),
                //        new Point2f(width, height),
                //        new Point2f(0, height)
                //    };

                //                            return points;
                //                        }
                //                        /*
                //                        // Добавляем найденные углы в список
                //                        cornersList.Add(corners);

                //                        // Выполняем калибровку камеры
                //                        Mat cameraMatrix = new Mat();
                //                        Mat distortionCoefficients = new Mat();

                //                        List<IEnumerable<Point3f>> objectPointsList = new List<IEnumerable<Point3f>> {
                //    objectPoints.Select(p => new Point3f(p.X, p.Y, 0)).ToList()
                //};

                //                        List<List<Point3f>> imagePointsList = cornersList
                //     .Select(corners => corners.Select(point2f => new Point3f(point2f.X, point2f.Y, 0)).ToList())
                //     .ToList();


                //                        List<Mat> objectPointsMatList = objectPointsList
                //                            .Select(points => new Mat(1, points.Count() * 3, MatType.CV_32F, points.SelectMany(p => new float[] { p.X, p.Y, 0 }).ToArray()))
                //                            .ToList();

                //                        List<Mat> imagePointsMatList = imagePointsList
                //                            .Select(corners => new Mat(1, corners.Count() * 3, MatType.CV_32F, corners.SelectMany(p => new float[] { p.X, p.Y, p.Z }).ToArray()))
                //                            .ToList();

                //                        StringBuilder sb = new StringBuilder();

                //                        if (objectPointsMatList.All(mat => mat.Rows == 1) && imagePointsMatList.All(mat => mat.Rows == 1))
                //                        {
                //                            foreach (var objMat in objectPointsMatList)
                //                            {
                //                                // Console.WriteLine($"Object Points Mat: {objMat}");
                //                                sb.AppendLine($"Object Points Mat: {objMat}");
                //                            }

                //                            foreach (var imgMat in imagePointsMatList)
                //                            {
                //                                // Console.WriteLine($"Image Points Mat: {imgMat}");
                //                                sb.AppendLine($"Image Points Mat: {imgMat}");
                //                            }
                //                            //MessageBox.Show( sb.ToString() );
                //                            // Теперь передаем objectPointsList и imagePointsList в CalibrateCamera
                //                            Mat[] rvecs;
                //                            Mat[] tvecs;
                //                            try
                //                            {
                //                                Cv2.CalibrateCamera(
                //                                    objectPointsMatList,
                //                                    imagePointsMatList,
                //                                    grayMat.Size(),
                //                                    cameraMatrix,
                //                                    distortionCoefficients,
                //                                    out rvecs,
                //                                    out tvecs
                //                                );
                //                            }
                //                            catch { }

                //                            // Применяем коррекцию дисторсии к изображению
                //                            //Mat undistortedImage = new Mat();
                //                            //Cv2.Undistort(matFrame1, undistortedImage, cameraMatrix, distortionCoefficients);

                //                            //// Преобразуем Mat обратно в BitmapSource
                //                            //BitmapSource undistortedBitmapSource = MatToBitmapImage(undistortedImage);
                //                            //cameraImage02.Source = undistortedBitmapSource;
                //                        }


                //                    // Находим углы шахматной доски
                //                    int chessboardRows = 8; // количество клеток по вертикали
                //                    int chessboardCols = 8; // количество клеток по горизонтали
                //                        */
                //                    }
                //                    /*
                //                    Mat chessboardImage = new Mat(new Size(chessboardCols, chessboardRows), MatType.CV_8UC3, new Scalar(255, 255, 255));

                //                    // Рисуем шахматную доску
                //                    for (int i = 0; i < chessboardRows; i++)
                //                    {
                //                        for (int j = 0; j < chessboardCols; j++)
                //                        {
                //                            if ((i + j) % 2 == 1)
                //                            {
                //                                Cv2.Rectangle(chessboardImage, new OpenCvSharp.Rect(j, i, 1, 1), new Scalar(0, 0, 0), thickness: -1);
                //                            }
                //                        }
                //                    }

                //                    // Размеры матрицы matFrame1
                //                    int frameWidth = matFrame1.Width;
                //                    int frameHeight = matFrame1.Height;

                //                    // Изменяем размер матрицы шахматной доски до размера matFrame1
                //                    Cv2.Resize(chessboardImage, chessboardImage, new Size(frameWidth, frameHeight));

                //                    // Создаем матрицу вращения и масштабирования
                //                    Mat rotationMatrix = Cv2.GetRotationMatrix2D(new Point2f(frameWidth / 2.0f, frameHeight / 2.0f), 30.0, 0.5); // Угол 30 градусов, масштаб 0.5

                //                    // Добавляем смещение к матрице преобразования
                //                    rotationMatrix.Set<double>(0, 2, rotationMatrix.At<double>(0, 2) - 50); // Смещение по оси X
                //                    rotationMatrix.Set<double>(1, 2, rotationMatrix.At<double>(1, 2) + 50); // Смещение по оси Y

                //                    // Применяем вращение и масштабирование к шахматной доске
                //                    Mat rotatedChessboardImage = new Mat();
                //                    Cv2.WarpAffine(chessboardImage, rotatedChessboardImage, rotationMatrix, new Size(frameWidth, frameHeight));

                //                    var scaleMatrix = new Mat(2, 3, MatType.CV_64F, new double[] { 1.0, 0.0, 0.0, 0.0, 0.45, 0.0 }); // Уменьшение верхней части изображения в 2 раза

                //                    // Применяем второе аффинное преобразование
                //                    Cv2.WarpAffine(rotatedChessboardImage, rotatedChessboardImage, scaleMatrix, rotatedChessboardImage.Size());

                //                    // Увеличиваем насыщенность цветов шахматной доски
                //                    Cv2.CvtColor(rotatedChessboardImage, rotatedChessboardImage, ColorConversionCodes.BGR2HSV);
                //                    Cv2.Split(rotatedChessboardImage, out Mat[] channels);
                //                    Cv2.Multiply(channels[1], new Scalar(20.0), channels[1]); // Умножаем канал насыщенности на 2.0 (может потребоваться настройка)
                //                    Cv2.Merge(channels, rotatedChessboardImage);
                //                    Cv2.CvtColor(rotatedChessboardImage, rotatedChessboardImage, ColorConversionCodes.HSV2BGR);

                //                    // Наложение шахматной доски поверх изображения
                //                    Mat resultMat = new Mat();
                //                    Cv2.BitwiseOr(matFrame1, rotatedChessboardImage, resultMat);

                //                    // Преобразуем Mat обратно в BitmapSource
                //                    BitmapSource resultBitmapSource = MatToBitmapImage(resultMat);
                //                    cameraImage02.Source = resultBitmapSource;

                //                    // Преобразуем Mat обратно в Bitmap
                //                    Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(resultMat);

                //                    // Преобразуем Bitmap в BitmapSource
                //                    BitmapSource alignedBitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                //                        bitmap.GetHbitmap(),
                //                        IntPtr.Zero,
                //                        System.Windows.Int32Rect.Empty,
                //                        BitmapSizeOptions.FromEmptyOptions());

                //                    // Устанавливаем выровненное изображение в качестве источника для элемента управления WPF
                //                    cameraImage02.Source = alignedBitmapSource;
                //                    */
                //                    /*
                //                    Cv2.GaussianBlur(matFrame1, matFrame1, new Size(5, 5), 0);
                //                    BitmapImage bitmapImage1 = MatToBitmapImage(matFrame1);
                //                    cameraImage02.Source = bitmapImage1;
                //                    // Находим углы шахматной доски
                //                    bool foundCorners = Cv2.FindChessboardCorners(matFrame1, new Size(chessboardCols, chessboardRows), out Point2f[] corners);

                //                    if (foundCorners && corners != null)
                //                    {
                //                        // Рисуем углы на изображении (для визуализации)
                //                        var colorMat = matFrame1.CvtColor(ColorConversionCodes.BGR2GRAY).CvtColor(ColorConversionCodes.GRAY2BGR);
                //                        var cornersInt = corners.Select(p => new OpenCvSharp.Point((int)p.X, (int)p.Y)).ToArray();
                //                        colorMat.Polylines(new OpenCvSharp.Point[][] { cornersInt }, true, Scalar.Red, 2);

                //                        // Получаем матрицу перспективного преобразования
                //                        Point2f[] targetCorners = new Point2f[]
                //                        {
                //        new Point2f(0, 0),
                //        new Point2f(matFrame1.Width - 1, 0),
                //        new Point2f(matFrame1.Width - 1, matFrame1.Height - 1),
                //        new Point2f(0, matFrame1.Height - 1)
                //                        };

                //                        Mat perspectiveTransform = Cv2.GetPerspectiveTransform(corners, targetCorners);

                //                        // Применяем перспективное преобразование
                //                        Cv2.WarpPerspective(matFrame1, matFrame1, perspectiveTransform, new Size(matFrame1.Width, matFrame1.Height));


                //                        BitmapImage bitmapImage = MatToBitmapImage(perspectiveTransform);
                //                        cameraImage02.Source = bitmapImage;

                //                    }
                //                    */
                //                    /*
                //                    bool foundCorners = Cv2.FindChessboardCorners(resultMat, new Size(chessboardCols, chessboardRows), out Point2f[] corners);

                //                    if (foundCorners)
                //                    {
                //                        // Рисуем углы на изображении (для визуализации)
                //                        var colorMat = matFrame1.CvtColor(ColorConversionCodes.GRAY2BGR);
                //                        var cornersInt = corners.Select(p => new OpenCvSharp.Point((int)p.X, (int)p.Y)).ToArray();
                //                        colorMat.Polylines(new OpenCvSharp.Point[][] { cornersInt }, true, Scalar.Red, 2);

                //                        // Получаем матрицу перспективного преобразования
                //                        Point2f[] targetCorners = new Point2f[]
                //                        {
                //                    new Point2f(0, 0),
                //                    new Point2f(matFrame1.Width - 1, 0),
                //                    new Point2f(matFrame1.Width - 1, matFrame1.Height - 1),
                //                    new Point2f(0, matFrame1.Height - 1)
                //                        };

                //                        Mat perspectiveTransform = Cv2.GetPerspectiveTransform(corners, targetCorners);

                //                        // Применяем перспективное преобразование
                //                        Cv2.WarpPerspective(matFrame1, matFrame1, perspectiveTransform, new Size(matFrame1.Width, matFrame1.Height));


                //                        // Преобразуем Mat в Bitmap
                //                        Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(matFrame1);

                //                        // Преобразуем Bitmap в BitmapSource
                //                        BitmapSource alignedBitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                //                            bitmap.GetHbitmap(),
                //                            IntPtr.Zero,
                //                            System.Windows.Int32Rect.Empty,
                //                            BitmapSizeOptions.FromEmptyOptions());

                //                        // Устанавливаем выровненное изображение в качестве источника для элемента управления WPF
                //                        cameraImage02.Source = alignedBitmapSource;

                //                    }*/
                //                });
                #endregion
            }
            else
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        cameraImage00.Source = null;
                        cameraImage01.Source = null;
                        cameraImage02.Source = null;
                    });
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Запускаем камеру при первом тике таймера
            if (!videoSource.IsRunning)
            {
                videoSource.Start();
            }
        }

        bool stop = false;
        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            // Запускаем таймер при нажатии на кнопку
            if (!stop)
            {
                //Thread thread1 = new Thread(() =>
                //{
                //    timer1.Start();
                //});
                //Thread thread2 = new Thread(() =>
                //{
                //    RotateImage1.timer2.Start();
                //});
                //thread1.Start();
                //videoSource.Start();
                //thread2.Start();
                timer1.Start();
                stop = true;
            }
            else
            {
                stop = false;
                timer1.Stop();
            }
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
