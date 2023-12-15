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
using System.Windows.Forms;
using LiveCharts.Wpf.Charts.Base;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.Collections.ObjectModel;

namespace camera.View
{
    /// <summary>
    /// Логика взаимодействия для CameraViewer.xaml
    /// </summary>
    public partial class CameraViewer : System.Windows.Controls.UserControl
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
                System.Windows.MessageBox.Show("Камеры не обнаружены");
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
        bool calibrate = false;
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
                    //cameraImage01.Source = bitmapImage;

                    ///
                    //BitmapImage bitmapImage1 = MatToBitmapImage(matFrame1);
                    //cameraImage02.Source = bitmapImage1;
                });

                #region Dispatcher 02
                Dispatcher.Invoke(() =>
                {
                    // Преобразование BitmapSource в Mat
                    Mat matFrame1 = BitmapSourceToMat(BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));

                    // Настройка параметров калибровки (замените на ваши реальные значения)
                    Size patternSize = new Size(9, 6); // размер шахматной доски
                    float squareSize = 1.0f; // размер квадрата на шахматной доске в метрах

                    StringBuilder sb = new StringBuilder();
                    sb.Clear();

                    Mat grayMat = new Mat();
                    Cv2.CvtColor(matFrame1, grayMat, ColorConversionCodes.BGR2GRAY);

                    bool foundCorners = Cv2.FindChessboardCorners(grayMat, patternSize, out Point2f[] corners);

                    //sb.AppendLine($"Количество найденных углов: {corners.Length} ");
                    //MessageBox.Show($"Количество найденных углов: {corners.Length} ");
                    foreach (var corner in corners)
                    {
                        sb.AppendLine($"Угол: {corner}");
                    }
                    //MessageBox.Show( sb.ToString() );

                    int horizontalCorners = (int)patternSize.Width;
                    int verticalCorners = (int)patternSize.Height;
                    try
                    {
                        //MessageBox.Show($"Количество углов по горизонтали: {horizontalCorners}+\nКоличество углов по вертикали: {verticalCorners}");
                        if (foundCorners)
                        {
                            #region DRAW Chess board Corners
                            // Рисуем углы на изображении (для визуализации)
                            //визуальное отображение точек шахматной доски
                            Cv2.DrawChessboardCorners(matFrame1, patternSize
                                , corners, foundCorners);
                            BitmapSource undistortedBitmapSource1 = MatToBitmapImage(matFrame1);
                            //cameraImage02.Source = undistortedBitmapSource1;

                            // Создаем область интереса (ROI) на основе найденных углов
                            OpenCvSharp.Rect roi = Cv2.BoundingRect(corners);
                            // Увеличьте область во всех направлениях
                            int expansionSize = 25;
                            roi.X -= expansionSize;
                            roi.Y -= expansionSize;
                            roi.Width += 2 * expansionSize;
                            roi.Height += 2 * expansionSize;


                            Mat matFrameTEST = BitmapSourceToMat(BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
                            Mat roiImage = new Mat(matFrameTEST, roi);
                            // Увеличиваем область интереса на весь экран
                            Mat enlargedImage = new Mat(matFrameTEST.Rows, matFrameTEST.Cols, matFrameTEST.Type());
                            Cv2.Resize(roiImage, enlargedImage, matFrameTEST.Size(), 0, 0, InterpolationFlags.Linear);

                            // Преобразуем Mat в BitmapSource и отобразим его
                            BitmapSource enlargedBitmapSource = MatToBitmapImage(enlargedImage);
                            cameraImage02.Source = enlargedBitmapSource;

                            patternSize = new Size(9, 6);
                            bool foundCornersTest = Cv2.FindChessboardCorners(enlargedImage, patternSize, out Point2f[] cornersTest);
                            if (foundCornersTest)
                            {
                                Cv2.DrawChessboardCorners(enlargedImage, patternSize
                               , cornersTest, foundCornersTest);
                                undistortedBitmapSource1 = MatToBitmapImage(enlargedImage);
                                cameraImage02.Source = undistortedBitmapSource1;

                                Point2f[] sortedCorners = cornersTest.OrderBy(p => p.Y).ToArray();

                                // Предполагаем, что доска имеет 9 точек в каждой строке
                                int pointsPerRow = 9;

                                // Разделяем углы на строки
                                List<List<Point2f>> rows1 = new List<List<Point2f>>();
                                for (int i = 0; i < sortedCorners.Length; i += pointsPerRow)
                                {
                                    List<Point2f> row = sortedCorners.Skip(i).Take(pointsPerRow).ToList();
                                    rows1.Add(row);
                                }

                                // Создаем новый список для измененных угловых точек
                                List<Point2f> modifiedCorners = new List<Point2f>();

                                // Выравниваем угловые точки в каждой строке
                                foreach (var row in rows1)
                                {
                                    // Найти минимальное и максимальное значение X
                                    float minX = row.Min(point => point.X);
                                    float maxX = row.Max(point => point.X);

                                    // Значение y первой точки в строке
                                    float firstPointY = row.First().Y;

                                    // Выровнять угловые точки
                                    for (int i = 0; i < row.Count; i++)
                                    {
                                        Point2f modifiedPoint = row[i];
                                        modifiedPoint.X = minX + i * ((maxX - minX) / (pointsPerRow - 1));
                                        modifiedPoint.Y = firstPointY; // Установить y первой точки
                                        modifiedCorners.Add(modifiedPoint);
                                    }
                                }
                                //foreach (var row in rows1)
                                //{
                                //    // Найти минимальное и максимальное значение X
                                //    float minX = row.Min(point => point.X);
                                //    float maxX = row.Max(point => point.X);

                                //    // Выровнять угловые точки
                                //    for (int i = 0; i < row.Count; i++)
                                //    {
                                //        Point2f modifiedPoint = row[i];
                                //        modifiedPoint.X = minX + i * ((maxX - minX) / (pointsPerRow - 1));
                                //        modifiedCorners.Add(modifiedPoint);
                                //    }
                                //}

                                /// Размеры шахматной доски
                                int rows = 9;
                                int cols = 6;

                                // Создаем массив для dstPoints
                                List<Point2f> dstPointsList = new List<Point2f>();

                                /////
                                //float minX = cornersTest.Min(point => point.X);
                                //float minY = cornersTest.Min(point => point.Y);

                                //float[] xValues = cornersTest.Select(point => point.X).ToArray();
                                //float[] yValues = cornersTest.Select(point => point.Y).ToArray();

                                //Array.Sort(xValues);
                                //Array.Sort(yValues);

                                //float secondMinX = xValues[1];
                                //float secondMinY = yValues[1];
                                //for (int row = 0; row < rows; row++)
                                //{
                                //    for (int col = 0; col < cols; col++)
                                //    {
                                //        dstPointsList.Add(new Point2f(col*minY, row*minX));
                                //    }
                                //}

                                //визуализация точек матриц шахмат и нового

                                //GraphicsAxesViewer g=new GraphicsAxesViewer(modifiedCorners, cornersTest.ToList() );
                                //g.ShowDialog();

                                IEnumerable<Point2f> dstPoints = dstPointsList.ToArray();

                                InputArray inputArray1 = InputArray.Create(cornersTest);
                                InputArray inputArray2 = InputArray.Create(modifiedCorners//dstPoints
                                    );

                                // Находим матрицу гомографии
                                Mat homographyMatrix = Cv2.FindHomography(inputArray1, inputArray2);

                                // Применяем гомографию к изображению
                                Mat correctedImage = new Mat();
                                Cv2.WarpPerspective(enlargedImage, correctedImage, homographyMatrix, new Size(correctedImage.Cols, correctedImage.Rows));

                                // Отображаем скорректированное изображение
                                undistortedBitmapSource1 = MatToBitmapImage(correctedImage);
                                cameraImage02.Source = undistortedBitmapSource1;
                            }
                            #endregion

                            #region CALIBRATION
                            /*
                            ///calibration
                            List<Mat> objectPoints = new List<Mat>();
                            List<Mat> imagePoints = new List<Mat>();
                            Size imageSize = new Size(matFrame1.Width, matFrame1.Height);
                            int k = 0;
                            var objectPointsMat = new Mat(patternSize.Width * patternSize.Height, 3, MatType.CV_32F);
                            var imagePointsMat = new Mat(patternSize.Width * patternSize.Height, 2, MatType.CV_32F);

                            for (int i = 0; i < patternSize.Height; i++)
                            {
                                for (int j = 0; j < patternSize.Width; j++)
                                {
                                    // objectPoints - координаты углов в мировой системе координат
                                    objectPointsMat.Set<float>(k, 0, j * squareSize);
                                    objectPointsMat.Set<float>(k, 1, i * squareSize);
                                    objectPointsMat.Set<float>(k, 2, 0.0f);

                                    // imagePoints - соответствующие 2D координаты на изображении
                                    imagePointsMat.Set<float>(k, 0, corners[k].X);
                                    imagePointsMat.Set<float>(k, 1, corners[k].Y);

                                    k++;
                                }
                            }

                            objectPoints.Add(objectPointsMat.Clone()); 
                            imagePoints.Add(imagePointsMat.Clone()); 


                            // Создайте объекты Mat для cameraMatrix и distCoeffs
                            Mat cameraMatrix = new Mat(3, 3, MatType.CV_64F);
                            Mat distCoeffs = new Mat(1, 2, MatType.CV_64F);

                            // Вызовите CalibrateCamera
                            Mat[] rvecs, tvecs;
                            double error = Cv2.CalibrateCamera(objectPoints, imagePoints, imageSize, cameraMatrix, distCoeffs, out rvecs, out tvecs);

                            Mat undistortedImage = new Mat();
                            Cv2.Undistort(matFrame1, undistortedImage, cameraMatrix, distCoeffs);

                            BitmapSource undistortedBitmapSource = MatToBitmapImage(undistortedImage);
                            cameraImage02.Source = undistortedBitmapSource;
                            */
                            #endregion

                            /*
                            ///homography
                            // Преобразование массива Point2f в Mat для исходных точек
                            Mat srcPointsMat = new Mat(corners.Length, 3, MatType.CV_32F);
                            for (int i = 0; i < corners.Length; i++)
                            {
                                srcPointsMat.Set(i, 0, corners[i].X);
                                srcPointsMat.Set(i, 1, corners[i].Y);
                            }

                            // Получение массива Point2f для верхнего вида
                            Size screenSize = new Size(cameraImage02.Width, cameraImage02.Height);
                            Point2f[] topViewPoints = GetTopViewPointsFromScreen(screenSize, patternSize);

                            // Проверка размерности массивов точек
                            if (topViewPoints.Length != corners.Length)
                            {
                                Console.WriteLine("Ошибка: Неверные размеры массивов точек для гомографии.");
                                return;
                            }

                            // Преобразование массива Point2f в Mat для верхних точек
                            Mat dstPointsMat = new Mat(topViewPoints.Length, 3, MatType.CV_32F);
                            for (int i = 0; i < topViewPoints.Length; i++)
                            {
                                dstPointsMat.Set(i, 0, topViewPoints[i].X);
                                dstPointsMat.Set(i, 1, topViewPoints[i].Y);
                            }
                            //string s = srcPointsMat.Size() + " ; \n" + dstPointsMat.Size();
                            //sb.Clear();
                            //MessageBox.Show(srcPointsMat.Size()+" ; \n"+ dstPointsMat.Size() );
                            // Получение матрицы гомографии
                            Mat homography = Cv2.FindHomography(srcPointsMat, dstPointsMat,HomographyMethods.Ransac);

                            //MessageBox.Show(" homography.Size() - " + homography.Size());
                            //Cv2.ImShow("Homography Matrix", homography);


                            // Создайте Mat для хранения результата гомографии
                            Mat warpedImage = new Mat();

                            // Примените гомографию к неискаженному изображению
                            Mat homographyFloat = new Mat(3, 3, MatType.CV_32F);
                            homography.ConvertTo(homographyFloat, MatType.CV_32F);

                            Cv2.WarpPerspective(enlargedImage, warpedImage, homographyFloat, new Size(enlargedImage.Width, enlargedImage.Height));

                            // Преобразуйте Mat в BitmapSource и отобразите его
                            BitmapSource warpedBitmapSource = MatToBitmapImage(warpedImage);
                            cameraImage02.Source = warpedBitmapSource;
                           */
                        }
                    }
                    catch { }
                });
                #endregion
            }
            else
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        //cameraImage00.Source = null;
                        //cameraImage01.Source = null;
                        cameraImage02.Source = null;
                    });
                }
                catch (Exception ex)
                {

                }
            }
        }

        private Point2f[] GetTopViewPointsFromScreen(Size screenSize, Size patternSize)
        {
            double squareWidth = (screenSize.Width) / (patternSize.Width - 1);
            double squareHeight = (screenSize.Height) / (patternSize.Height - 1);

            Point2f[] points = new Point2f[patternSize.Width * patternSize.Height];

            int index = 0;
            for (int i = 0; i < patternSize.Height; i++)
            {
                for (int j = 0; j < patternSize.Width; j++)
                {
                    points[index++] = new Point2f((float)(j * squareWidth), (float)(i * squareHeight));
                }
            }

            return points;
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
