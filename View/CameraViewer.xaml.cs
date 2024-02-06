using AForge.Video.DirectShow;
using AForge.Video;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using OpenCvSharp.Extensions;
using System.IO;
using System.Windows.Media;
using Size = OpenCvSharp.Size;
using System.Text;
using camera.Model;
using camera.Interfaces;
using System.Windows.Forms;

namespace camera.View
{
    /// <summary>
    /// Логика взаимодействия для CameraViewer.xaml
    /// </summary>
    public partial class CameraViewer : System.Windows.Controls.UserControl , IConverterCamera
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private DispatcherTimer timer1;

        public CameraViewer()
        {
            InitializeComponent();
            InitializeCamera();
            Unloaded += CameraViewer_Unloaded;
        }
        public void InitializeCamera()
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
        public Mat BitmapSourceToMat(BitmapSource source)
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
        public BitmapImage MatToBitmapImage(Mat mat)
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
        CameraParameters cameraParameters = new CameraParameters();
        private readonly object lockObject = new object();
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
                // Обновляем изображение в Image
                if (stop && !new_stop)
                {
                    ///участок код для контуров
                    Mat matFrame = BitmapSourceToMat(BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
                    Cv2.CvtColor(matFrame, matFrame, ColorConversionCodes.BGR2GRAY);
                    Cv2.Canny(matFrame, matFrame, 50, 150);
                    ///
                    Dispatcher.Invoke(() =>
                    {
                        if (IsLoaded)  // Или IsClosed, в зависимости от вашего окна
                        {
                            ///обычная камера
                            cameraImage00.Source = BitmapToImageSource((Bitmap)eventArgs.Frame.Clone());

                            ///выделение контуров
                            BitmapImage bitmapImage = MatToBitmapImage(matFrame);
                        }
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

                        bool foundCorners = Cv2.FindChessboardCorners(matFrame1, patternSize, out Point2f[] corners);
                        int horizontalCorners = (int)patternSize.Width;
                        int verticalCorners = (int)patternSize.Height;
                        try
                        {
                            if (foundCorners)
                            {
                                #region DRAW Chess board Corners

                                // Рисуем углы на изображении (для визуализации)
                                //визуальное отображение точек шахматной доски
                                Cv2.DrawChessboardCorners(matFrame1, patternSize
                                    , corners, foundCorners);
                                BitmapSource undistortedBitmapSource1 = MatToBitmapImage(matFrame1);
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
                                bool foundCornersTest = Cv2.FindChessboardCorners(matFrameTEST//enlargedImage
                                    , patternSize, out Point2f[] cornersTest);
                                if (foundCornersTest)
                                {
                                    Cv2.DrawChessboardCorners(matFrameTEST//enlargedImage
                                        , patternSize
                                   , cornersTest, foundCornersTest);
                                    undistortedBitmapSource1 = MatToBitmapImage(matFrameTEST//enlargedImage
                                        );
                                    cameraImage02.Source = undistortedBitmapSource1;

                                    /*
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
                                    //foreach (var row in rows1)
                                    for (int j = 0; j < rows1.Count; j++)
                                    {
                                        // Найти минимальное и максимальное значение X
                                        float minX = rows1[j].Min(point => point.X);
                                        float maxX = rows1[j].Max(point => point.X);

                                        // Значение y первой точки в строке
                                        float firstPointY = rows1[j].First().Y;

                                        var row = rows1[j];
                                        // Выровнять угловые точки
                                        for (int i = 0; i < row.Count; i++)
                                        {
                                            Point2f modifiedPoint = row[i];
                                            modifiedPoint.X = minX + i * ((maxX - minX) / (pointsPerRow - 1));
                                            try
                                            {
                                                var previousRow = rows1[j - 1];
                                                Point2f modifiedPointTRY = previousRow[i];
                                                modifiedPoint.X = modifiedPointTRY.X;
                                            }
                                            catch (Exception ex)
                                            {

                                            }
                                            modifiedPoint.Y = firstPointY; // Установить y первой точки
                                            modifiedCorners.Add(modifiedPoint);
                                        }
                                    }
                                    */

                                    //визуализация точек матриц шахмат и нового
                                    //GraphicsAxesViewer g=new GraphicsAxesViewer(modifiedCorners, cornersTest.ToList() );
                                    //g.ShowDialog();
                                    #region getMatrixIdealChessDesk
                                    Mat matFrame = Cv2.ImRead("C:\\projects\\sharpCameraPrinter\\camera\\idealChesss.png");
                                    Dispatcher.Invoke(() =>
                                    {

                                        //получили массив точек идеального изображения

                                        var patternSize = new Size(9, 6);
                                        bool foundCornersTest = Cv2.FindChessboardCorners(matFrame, patternSize, out Point2f[] cornersTest);
                                        if (foundCornersTest)
                                        {
                                            Cv2.DrawChessboardCorners(matFrame, patternSize
                                           , cornersTest, foundCornersTest);

                                            undistortedBitmapSource1 = MatToBitmapImage(matFrame);

                                            ////получили массив точек идеального изображения
                                            idealChessDesk = cornersTest;
                                            cameraImageIdeal.Source = null;
                                            cameraImageIdeal.Source = undistortedBitmapSource1;
                                        }
                                    });
                                    #endregion
                                    InputArray inputArray1 = InputArray.Create(cornersTest);
                                    InputArray inputArray2 = InputArray.Create(idealChessDesk
                                        //modifiedCorners
                                        );

                                    // Находим матрицу гомографии
                                    Mat homographyMatrix = Cv2.FindHomography(inputArray1, inputArray2);

                                    //запоминаем матрицу
                                    homographyMatrixGlobalforImageWithoutChessDesk = homographyMatrix;

                                    // Применяем гомографию к изображению
                                    Mat correctedImage = new Mat();
                                    Cv2.WarpPerspective(matFrameTEST//enlargedImage
                                        , correctedImage, homographyMatrix, new Size(correctedImage.Cols, correctedImage.Rows));

                                    double scale_factor = 0.5;

                                    // Получение новых размеров с сохранением пропорций
                                    int newWidth = (int)(matFrame1.Width * scale_factor);
                                    int newHeight = (int)(matFrame1.Height * scale_factor);

                                    // Уменьшение изображения с интерполяцией Inter.Linear
                                    Mat resizedImage = new Mat();
                                    Cv2.Resize(correctedImage, resizedImage, new Size(newWidth, newHeight), interpolation: InterpolationFlags.Linear);


                                    // Отображаем скорректированное изображение
                                    undistortedBitmapSource1 = MatToBitmapImage(resizedImage);
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
                        catch (Exception ex)
                        {
                            string s = ex.Message;
                        }
                    });
                    #endregion
                }
                if (!stop && new_stop)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Mat matFrame0 = BitmapSourceToMat(BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
                        var undistortedBitmapSource1 = MatToBitmapImage(matFrame0);
                        cameraImage00.Source = null;
                        cameraImage00.Source = undistortedBitmapSource1;
                    });

                    Mat matFrame = BitmapSourceToMat(BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
                    Mat correctedImage = new Mat();
                    Cv2.WarpPerspective(matFrame, correctedImage, homographyMatrixGlobalforImageWithoutChessDesk, new Size(correctedImage.Cols, correctedImage.Rows), flags: InterpolationFlags.Linear);

                    // Отображаем скорректированное изображение
                    Dispatcher.Invoke(() =>
                    {
                        var undistortedBitmapSource1 = MatToBitmapImage(correctedImage);
                        cameraImage02.Source = null;
                        cameraImage02.Source = undistortedBitmapSource1;

                        double scaleFactor = scaleScrollBar.Value;

                        // Получение BitmapSource из Source элемента управления cameraImage02
                        BitmapSource bitmapSource = cameraImage02.Source as BitmapSource;

                        if (bitmapSource != null)
                        {
                            // Создаем TransformedBitmap для изменения размеров
                            TransformedBitmap transformedBitmap = new TransformedBitmap(bitmapSource,
                                new ScaleTransform(scaleFactor, scaleFactor));

                            // Конвертация TransformedBitmap в Mat
                            Mat matNew = transformedBitmap.ToMat();

                            // Отображение уменьшенного изображения
                            undistortedBitmapSource1 = MatToBitmapImage(matNew);

                            // Установка нового изображения в элемент управления Image
                            cameraImage02.Source = undistortedBitmapSource1;

                            // Обновление размеров элемента управления Image
                            cameraImage02.Width = bitmapSource.PixelWidth * scaleFactor;
                            cameraImage02.Height = bitmapSource.PixelHeight * scaleFactor;
                        }
                    });
                }
                if (standart_stop)
                {
                    //Mat matFrame = BitmapSourceToMat(BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
                    Mat matFrame = Cv2.ImRead("C:\\projects\\sharpCameraPrinter\\camera\\idealChesss.png");
                    Dispatcher.Invoke(() =>
                    {
                        var undistortedBitmapSource1 = MatToBitmapImage(matFrame);
                        cameraImage00.Source = null;
                        cameraImage00.Source = undistortedBitmapSource1;

                        //получили массив точек идеального изображения
                        //selected_contur(matFrame);
                        var patternSize = new Size(9, 6);
                        bool foundCornersTest = Cv2.FindChessboardCorners(matFrame, patternSize, out Point2f[] cornersTest);
                        if (foundCornersTest)
                        {
                            Cv2.DrawChessboardCorners(matFrame, patternSize
                           , cornersTest, foundCornersTest);

                            undistortedBitmapSource1 = MatToBitmapImage(matFrame);
                            cameraImage02.Source = null;
                            cameraImage02.Source = undistortedBitmapSource1;

                            //получили массив точек идеального изображения
                            idealChessDesk = cornersTest;
                            cameraImageIdeal.Source = null;
                            cameraImageIdeal.Source = undistortedBitmapSource1;
                            //selected_contur(matFrame);
                        }
                    });
                }
            
        }
        Point2f[] idealChessDesk;
        Mat homographyMatrixGlobalforImageWithoutChessDesk;// =new Mat();
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Запускаем камеру при первом тике таймера
            if (!videoSource.IsRunning)
            {
                videoSource.Start();
            }
        }

        bool stop = false;
        bool new_stop = false;
        bool standart_stop = false;
        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            // Запускаем таймер при нажатии на кнопку
            if (!stop)
            {
                new_stop = false;
                timer1.Start();
                stop = true;
            }
            if (new_stop)
            {
                stop = false;
                timer1.Stop();

                timer1.Start();
            }
        }
        private void btnSaveParamsFromCameraImage(object sender, RoutedEventArgs e)
        {
            if (new_stop)
            {
                new_stop = false;
            }
            if (homographyMatrixGlobalforImageWithoutChessDesk != null)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "YAML files (*.yml)|*.yml|All files (*.*)|*.*";
                    saveFileDialog.Title = "Выберите путь для сохранения матрицы гомографии";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = saveFileDialog.FileName;

                        using (FileStorage fs = new FileStorage(filePath, FileStorage.Modes.Write))
                        {
                            fs.Write("homographyMatrixGlobalforImageWithoutChessDesk", homographyMatrixGlobalforImageWithoutChessDesk);
                        }

                        System.Windows.MessageBox.Show("Матрица сохранена успешно!");
                    }
                }
                new_stop = true;
                timer1.Stop();

                timer1.Start();
                refactor_image();
            }
        }
        private void VideoSource_Frame_with_LoadParams(object sender, NewFrameEventArgs eventArgs) 
        {
            Dispatcher.Invoke(() =>
            {
                Mat matFrame0 = BitmapSourceToMat(BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
                var undistortedBitmapSource1 = MatToBitmapImage(matFrame0);
                cameraImage00.Source = null;
                cameraImage00.Source = undistortedBitmapSource1;
            });

            Mat matFrame = BitmapSourceToMat(BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
            Mat correctedImage = new Mat();
            if (homographyMatrixGlobalforImageWithoutChessDesk.Type() != MatType.CV_32F && homographyMatrixGlobalforImageWithoutChessDesk.Type() != MatType.CV_64F)
            {
                return;
            }
            if (homographyMatrixGlobalforImageWithoutChessDesk.Rows != 3 || homographyMatrixGlobalforImageWithoutChessDesk.Cols != 3)
            {
                return;
            }
            Cv2.WarpPerspective(matFrame, correctedImage, homographyMatrixGlobalforImageWithoutChessDesk, new Size(correctedImage.Cols, correctedImage.Rows), flags: InterpolationFlags.Linear);

            // Отображаем скорректированное изображение
            Dispatcher.Invoke(() =>
            {
                var undistortedBitmapSource1 = MatToBitmapImage(correctedImage);
                cameraImage02.Source = null;
                cameraImage02.Source = undistortedBitmapSource1;

                double scaleFactor = scaleScrollBar.Value;

                // Получение BitmapSource из Source элемента управления cameraImage02
                BitmapSource bitmapSource = cameraImage02.Source as BitmapSource;

                if (bitmapSource != null)
                {
                    // Создаем TransformedBitmap для изменения размеров
                    TransformedBitmap transformedBitmap = new TransformedBitmap(bitmapSource,
                        new ScaleTransform(scaleFactor, scaleFactor));

                    // Конвертация TransformedBitmap в Mat
                    Mat matNew = transformedBitmap.ToMat();

                    // Отображение уменьшенного изображения
                    undistortedBitmapSource1 = MatToBitmapImage(matNew);

                    // Установка нового изображения в элемент управления Image
                    cameraImage02.Source = undistortedBitmapSource1;

                    // Обновление размеров элемента управления Image
                    cameraImage02.Width = bitmapSource.PixelWidth * scaleFactor;
                    cameraImage02.Height = bitmapSource.PixelHeight * scaleFactor;
                }
            });
        }
        private void LoadMatrixButton_Click(object sender, EventArgs e)
        {
            try
            {
                load_matrix();
            }
            catch (Exception ex){
                System.Windows.MessageBox.Show("Error : \n"+ex);
            }
        }
        void load_matrix()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "YAML files (*.yml)|*.yml|All files (*.*)|*.*";
                openFileDialog.Title = "Load Matrix File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;

                    using (FileStorage fsRead = new FileStorage(filePath, FileStorage.Modes.Read))
                    {
                        using (Mat mat = fsRead["homographyMatrixGlobalforImageWithoutChessDesk"].ReadMat())
                        {
                            if (mat != null)
                            {
                                homographyMatrixGlobalforImageWithoutChessDesk = mat.Clone();
                            }
                            else
                            {
                                System.Windows.MessageBox.Show("null!");
                                return;
                            }
                        }
                    }

                    System.Windows.Forms.MessageBox.Show("Матрица загружена успешно.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    videoSource.NewFrame -= VideoSource_NewFrame;
                    videoSource.NewFrame += VideoSource_Frame_with_LoadParams;
                    timer1.Start();
                }
            }
            if (!stop)
            {
                new_stop = false;
                timer1.Start();
                stop = true;
            }
        }
        void click_get_standart_param(object sender, RoutedEventArgs e)
        {
            if (!standart_stop)
            {
                standart_stop = true;
                timer1.Start();
            }
            else
            {
                standart_stop = false;
                timer1.Stop();
            }
        }
        void refactor_image()
        {

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
        public BitmapImage BitmapToImageSource(Bitmap bitmap)
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
        public void CameraViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            // код обработки завершения использования UserControl
            // Освобождение ресурсов, отписка от событий и прочее
            if (videoSource != null)
            {
                videoSource.NewFrame -= VideoSource_NewFrame;
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }

            if (timer1 != null)
            {
                timer1.Tick -= Timer_Tick;
                timer1.Stop();
            }
        }
        public void StopCamera()
        {
            if (videoSource != null)
            {
                videoSource.NewFrame -= VideoSource_NewFrame;
                videoSource.SignalToStop();
            }

            if (timer1 != null)
            {
                timer1.Tick -= Timer_Tick;
                timer1.Stop();
            }
        }
    }
}
