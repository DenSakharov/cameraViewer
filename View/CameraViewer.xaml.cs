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
using System.Windows.Forms;
using camera.Converters;
using camera.Delegates;
using System.Diagnostics;

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

            //delegates initialization
            DelegateUtils.VideoSource_NewFrame_delegate_EventHandler delegate_VideoSource = VideoSource_NewFrame_Standart_View//VideoSource_NewFrame
                ;
            DelegateUtils.Timer_Tick_delegate_EventHandler delegate_Timer_Tick = Timer_Tick;

            (videoDevices, videoSource, timer1) = ImageConvert.InitializeCamera(videoDevices, videoSource, timer1, delegate_VideoSource, delegate_Timer_Tick);
            if (videoDevices == null || videoDevices.Count == 0)
            {
                // Выводим сообщение о том, что камеры не обнаружены
                System.Windows.MessageBox.Show("Камеры не обнаружены");
                return;
            }
            Unloaded += CameraViewer_Unloaded;
        }
        /// <summary>
        /// Обработчик события для обработки кадров с видеоисточника с целью обнаружения и коррекции искажений, вызванных перспективой камеры, при условии использования калибровочной шахматной доски.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие.</param>
        /// <param name="eventArgs">Аргументы, содержащие новый кадр из видеоисточника.</param>
        private void VideoSource_NewFrame_Standart_View(object sender, NewFrameEventArgs eventArgs)
        {
            #region Dispatcher 02
            Dispatcher.Invoke(() =>
            {
                // Преобразование BitmapSource в Mat
                Mat matFrame1 = ImageConvert.BitmapSourceToMat(ImageConvert.BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));

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
                        BitmapSource undistortedBitmapSource1 = ImageConvert.MatToBitmapImage(matFrame1);
                        // Создаем область интереса (ROI) на основе найденных углов
                        OpenCvSharp.Rect roi = Cv2.BoundingRect(corners);
                        // Увеличьте область во всех направлениях
                        int expansionSize = 25;
                        roi.X -= expansionSize;
                        roi.Y -= expansionSize;
                        roi.Width += 2 * expansionSize;
                        roi.Height += 2 * expansionSize;

                        Mat matFrameTEST = ImageConvert.BitmapSourceToMat(ImageConvert.BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
                        Mat roiImage = new Mat(matFrameTEST, roi);
                        // Увеличиваем область интереса на весь экран
                        Mat enlargedImage = new Mat(matFrameTEST.Rows, matFrameTEST.Cols, matFrameTEST.Type());
                        Cv2.Resize(roiImage, enlargedImage, matFrameTEST.Size(), 0, 0, InterpolationFlags.Linear);

                        // Преобразуем Mat в BitmapSource и отобразим его
                        BitmapSource enlargedBitmapSource = ImageConvert.MatToBitmapImage(enlargedImage);
                        cameraImage02.Source = enlargedBitmapSource;

                        patternSize = new Size(9, 6);
                        bool foundCornersTest = Cv2.FindChessboardCorners(matFrameTEST//enlargedImage
                            , patternSize, out Point2f[] cornersTest);
                        if (foundCornersTest)
                        {
                            Cv2.DrawChessboardCorners(matFrameTEST//enlargedImage
                                , patternSize
                           , cornersTest, foundCornersTest);
                            undistortedBitmapSource1 = ImageConvert.MatToBitmapImage(matFrameTEST//enlargedImage
                                );
                            cameraImage02.Source = undistortedBitmapSource1;
                            #region getMatrixIdealChessDesk
                            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pictures", "idealChesss.png");
                            Mat matFrame = Cv2.ImRead(imagePath//"C:\\projects\\sharpCameraPrinter\\camera\\idealChesss.png"
                                );
                            Dispatcher.Invoke(() =>
                            {

                                //получили массив точек идеального изображения

                                var patternSize = new Size(9, 6);
                                bool foundCornersTest = Cv2.FindChessboardCorners(matFrame, patternSize, out Point2f[] cornersTest);
                                if (foundCornersTest)
                                {
                                    Cv2.DrawChessboardCorners(matFrame, patternSize
                                   , cornersTest, foundCornersTest);

                                    undistortedBitmapSource1 = ImageConvert.MatToBitmapImage(matFrame);

                                    ////получили массив точек идеального изображения
                                    idealChessDesk = cornersTest;
                                    //cameraImageIdeal.Source = null;
                                    //cameraImageIdeal.Source = undistortedBitmapSource1;
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
                            buttonSaveParams.IsEnabled = true;
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
                            undistortedBitmapSource1 = ImageConvert.MatToBitmapImage(resizedImage);
                            cameraImage02.Source = undistortedBitmapSource1;
                        }
                        #endregion
                    }
                    else
                    {
                        cameraImage02.Source = null;
                    }
                }
                catch (Exception ex)
                {
                    string s = ex.Message;
                }
            });
            #endregion
        }

        /*
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                if (cameraImage02.Source == null)
                {
                    label_image_source_2.Content = "Изображение с шахматной доской не найдено";
                }
                else
                {
                    label_image_source_2.Content = "";
                }
            });
            // Обновляем изображение в Image
            if (stop && !new_stop)
            {
                #region Dispatcher 02
                Dispatcher.Invoke(() =>
                {
                    // Преобразование BitmapSource в Mat
                    Mat matFrame1 = ImageConvert.BitmapSourceToMat(ImageConvert.BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));

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
                            BitmapSource undistortedBitmapSource1 = ImageConvert.MatToBitmapImage(matFrame1);
                            // Создаем область интереса (ROI) на основе найденных углов
                            OpenCvSharp.Rect roi = Cv2.BoundingRect(corners);
                            // Увеличьте область во всех направлениях
                            int expansionSize = 25;
                            roi.X -= expansionSize;
                            roi.Y -= expansionSize;
                            roi.Width += 2 * expansionSize;
                            roi.Height += 2 * expansionSize;

                            Mat matFrameTEST = ImageConvert.BitmapSourceToMat(ImageConvert.BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
                            Mat roiImage = new Mat(matFrameTEST, roi);
                            // Увеличиваем область интереса на весь экран
                            Mat enlargedImage = new Mat(matFrameTEST.Rows, matFrameTEST.Cols, matFrameTEST.Type());
                            Cv2.Resize(roiImage, enlargedImage, matFrameTEST.Size(), 0, 0, InterpolationFlags.Linear);

                            // Преобразуем Mat в BitmapSource и отобразим его
                            BitmapSource enlargedBitmapSource = ImageConvert.MatToBitmapImage(enlargedImage);
                            cameraImage02.Source = enlargedBitmapSource;

                            patternSize = new Size(9, 6);
                            bool foundCornersTest = Cv2.FindChessboardCorners(matFrameTEST//enlargedImage
                                , patternSize, out Point2f[] cornersTest);
                            if (foundCornersTest)
                            {
                                Cv2.DrawChessboardCorners(matFrameTEST//enlargedImage
                                    , patternSize
                               , cornersTest, foundCornersTest);
                                undistortedBitmapSource1 = ImageConvert.MatToBitmapImage(matFrameTEST//enlargedImage
                                    );
                                cameraImage02.Source = undistortedBitmapSource1;
                                #region getMatrixIdealChessDesk
                                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pictures", "idealChesss.png");
                                Mat matFrame = Cv2.ImRead(imagePath//"C:\\projects\\sharpCameraPrinter\\camera\\idealChesss.png"
                                    );
                                Dispatcher.Invoke(() =>
                                {

                                    //получили массив точек идеального изображения

                                    var patternSize = new Size(9, 6);
                                    bool foundCornersTest = Cv2.FindChessboardCorners(matFrame, patternSize, out Point2f[] cornersTest);
                                    if (foundCornersTest)
                                    {
                                        Cv2.DrawChessboardCorners(matFrame, patternSize
                                       , cornersTest, foundCornersTest);

                                        undistortedBitmapSource1 = ImageConvert.MatToBitmapImage(matFrame);

                                        ////получили массив точек идеального изображения
                                        idealChessDesk = cornersTest;
                                        //cameraImageIdeal.Source = null;
                                        //cameraImageIdeal.Source = undistortedBitmapSource1;
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
                                buttonSaveParams.IsEnabled = true;
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
                                undistortedBitmapSource1 = ImageConvert.MatToBitmapImage(resizedImage);
                                cameraImage02.Source = undistortedBitmapSource1;
                            }
                            #endregion
                        }
                        else
                        {
                            cameraImage02.Source = null;
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
                    Mat matFrame = ImageConvert.BitmapSourceToMat(ImageConvert.BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
                    Mat correctedImage = new Mat();
                    Cv2.WarpPerspective(matFrame, correctedImage, homographyMatrixGlobalforImageWithoutChessDesk, new Size(correctedImage.Cols, correctedImage.Rows), flags: InterpolationFlags.Linear);

                    var undistortedBitmapSource1 = ImageConvert.MatToBitmapImage(correctedImage);
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
                        undistortedBitmapSource1 = ImageConvert.MatToBitmapImage(matNew);

                        // Установка нового изображения в элемент управления Image
                        cameraImage02.Source = undistortedBitmapSource1;

                        // Обновление размеров элемента управления Image
                        cameraImage02.Width = bitmapSource.PixelWidth * scaleFactor;
                        cameraImage02.Height = bitmapSource.PixelHeight * scaleFactor;
                    }
                });
            }
        }
        */

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
        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (start_stop_btn.Content == "Stop Camera")
            {
                videoSource.NewFrame -= VideoSource_NewFrame_Standart_View;
                videoSource.NewFrame -= VideoSource_Frame_with_LoadParams;
                cameraImage02.Source = null;
                timer1.Stop();

                start_stop_btn.Content = "Start Camera";
                return;
            }
            // Запускаем таймер при нажатии на кнопку
            if (!stop)
            {
                //new_stop = false;
                videoSource.NewFrame += VideoSource_NewFrame_Standart_View;
                videoSource.NewFrame -= VideoSource_Frame_with_LoadParams;
                timer1.Start();
                //stop = true;
                start_stop_btn.Content = "Stop Camera";
            }
            if (new_stop)
            {
                //stop = false;
                timer1.Stop();
                videoSource.NewFrame -= VideoSource_NewFrame_Standart_View;
                videoSource.NewFrame += VideoSource_Frame_with_LoadParams;
                timer1.Start();
                start_stop_btn.Content = "Start Camera";
            }
        }
        void unsubscribed()
        {
            // Получаем список всех обработчиков события NewFrame
            Delegate[] eventHandlers = videoSource.NewFrame.GetInvocationList();

            // Перебираем каждый обработчик и отписываемся от него
            foreach (var handler in eventHandlers)
            {
                videoSource.NewFrame -= (NewFrameEventHandler)handler;
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
                //new_stop = true;
                videoSource.NewFrame -= VideoSource_NewFrame_Standart_View;
                videoSource.NewFrame += VideoSource_Frame_with_LoadParams;
                timer1.Stop();
                timer1.Start();
            }
        }
        /// <summary>
        /// Метод для видеопотока с обработкой библиотеки и матрицы гомографии
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void VideoSource_Frame_with_LoadParams(object sender, NewFrameEventArgs eventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                Mat matFrame = ImageConvert.BitmapSourceToMat(ImageConvert.BitmapToImageSource((Bitmap)eventArgs.Frame.Clone()));
                Mat correctedImage = new Mat();
                Cv2.WarpPerspective(matFrame, correctedImage, homographyMatrixGlobalforImageWithoutChessDesk, new Size(correctedImage.Cols, correctedImage.Rows), flags: InterpolationFlags.Linear);

                // Отображаем скорректированное изображение
                var undistortedBitmapSource1 = ImageConvert.MatToBitmapImage(correctedImage);

                double scaleFactor = scaleScrollBar.Value;

                // Получение BitmapSource из Source элемента управления cameraImage02
                BitmapSource bitmapSource = undistortedBitmapSource1//cameraImage02.Source 
                as BitmapSource;

                if (bitmapSource != null)
                {
                    // Создаем TransformedBitmap для изменения размеров
                    TransformedBitmap transformedBitmap = new TransformedBitmap(bitmapSource,
                        new ScaleTransform(scaleFactor, scaleFactor));

                    // Конвертация TransformedBitmap в Mat
                    Mat matNew = transformedBitmap.ToMat();

                    // Отображение уменьшенного изображения
                    undistortedBitmapSource1 = ImageConvert.MatToBitmapImage(matNew);

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
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error : \n" + ex);
            }
        }
        /// <summary>
        /// Метод загрузки готовой сохраненной матрицы гомографии
        /// </summary>
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

                    //System.Windows.Forms.MessageBox.Show("Матрица загружена успешно.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    //переключение на обработку видео нового метода с заранее загруженной матрицей гомографии 
                    videoSource = null;
                    videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                    videoSource.NewFrame += VideoSource_Frame_with_LoadParams;
                    videoSource.Start();
                }
            }
            if (!stop)
            {
                new_stop = false;
                timer1.Start();
                stop = true;
            }
        }
        #region
        string selectedDirectory = "";
        /// <summary>
        /// Метод для фотографии и скриншота с камеры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void take_picture(object sender, EventArgs e)
        {
            if (selectedDirectory == "")
            {
                // Отображение диалогового окна выбора директории
                var dialog = new System.Windows.Forms.FolderBrowserDialog();

                var temp = cameraImage02.Source as BitmapSource;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                // Проверка результата диалогового окна
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    selectedDirectory = dialog.SelectedPath;

                    // Создание уникального имени файла на основе времени
                    string fileName = $"image_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                    string filePath = Path.Combine(selectedDirectory, fileName);

                    // Сохранение изображения
                    SaveImage(temp, filePath);
                }
            }
            else
            {
                string fileName = $"image_{DateTime.Now:yyyyMMddHHmmss}.bmp";
                string filePath = Path.Combine(selectedDirectory, fileName);
                SaveImage(cameraImage02.Source as BitmapSource, filePath);
            }
        }

        private void SaveImage(BitmapSource bitmapSource, string filePath)
        {
            try
            {
                // Создание кодировщика JPEG
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                // Запись изображения в файл
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }
                //System.Windows.MessageBox.Show("Изображение сохранено.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при сохранении изображения: {ex.Message}");
            }
        }
        /// <summary>
        /// Остановка видеопотока при выключении приложения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Останавливаем камеру при закрытии окна
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
                Process currentProcess = Process.GetCurrentProcess();

                // Получить все потоки процесса
                foreach (ProcessThread thread in currentProcess.Threads)
                {
                    // Остановить поток
                    thread.Dispose();
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        /// <summary>
        /// События завершения видео потока
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CameraViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            // код обработки завершения использования UserControl
            // Освобождение ресурсов, отписка от событий и прочее
            if (videoSource != null)
            {
                videoSource.Stop();
                //videoSource.NewFrame -= VideoSource_NewFrame;
                videoSource.SignalToStop();
                videoSource.WaitForStop();
                videoSource = null;
            }

            if (timer1 != null)
            {
                timer1.Tick -= Timer_Tick;
                timer1.Stop();
            }
        }
        /// <summary>
        /// Метод для заверешения видео потока при закрытии приложения
        /// </summary>
        public void StopCamera()
        {
            if (videoSource != null)
            {
                //videoSource.NewFrame -= VideoSource_NewFrame;
                videoSource.SignalToStop();
                videoSource.Stop();
                videoSource = null;
            }

            if (timer1 != null)
            {
                timer1.Tick -= Timer_Tick;
                timer1.Stop();
            }
        }
        #endregion
    }
}
