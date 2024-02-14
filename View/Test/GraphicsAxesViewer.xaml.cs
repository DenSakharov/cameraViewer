using LiveCharts.Wpf;
using LiveCharts;
using OpenCvSharp;
using LiveCharts.Defaults;
using System.Drawing;

namespace camera.View
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class GraphicsAxesViewer : System.Windows.Window
    {
        public GraphicsAxesViewer(List<Point2f> dstPointsList, List<Point2f> dstPointsList2)
        {
            InitializeComponent();

            // Создаем ChartValues для X и Y для первой коллекции точек
            ChartValues<double> xValues1 = new ChartValues<double>();
            ChartValues<double> yValues1 = new ChartValues<double>();

            // Заполняем значения X и Y из первой коллекции точек
            foreach (var point in dstPointsList)
            {
                xValues1.Add(point.X+1);
                yValues1.Add(point.Y-1);
            }

            // Создаем ScatterSeries для первой коллекции точек
            ScatterSeries scatterSeries1 = new ScatterSeries
            {
                Values = new ChartValues<ObservablePoint>(),
                PointGeometry = DefaultGeometries.Circle, // Размер точек
                Title = "Points1"                           // Заголовок для легенды
            };

            // Заполняем ScatterSeries точками из первой коллекции
            for (int i = 0; i < xValues1.Count; i++)
            {
                scatterSeries1.Values.Add(new ObservablePoint(xValues1[i], yValues1[i]));
            }

            // Создаем ChartValues для X и Y для второй коллекции точек
            ChartValues<double> xValues2 = new ChartValues<double>();
            ChartValues<double> yValues2 = new ChartValues<double>();

            // Заполняем значения X и Y из второй коллекции точек
            foreach (var point in dstPointsList2)
            {
                xValues2.Add(point.X);
                yValues2.Add(point.Y);
            }

            // Создаем ScatterSeries для второй коллекции точек
            ScatterSeries scatterSeries2 = new ScatterSeries
            {
                Values = new ChartValues<ObservablePoint>(),
                PointGeometry = DefaultGeometries.Circle, // Размер точек
                Title = "Points2",                         // Заголовок для легенды
                Fill = System.Windows.Media.Brushes.Red,                         // Цвет точек для второй коллекции
            };

            // Заполняем ScatterSeries точками из второй коллекции
            for (int i = 0; i < xValues2.Count; i++)
            {
                scatterSeries2.Values.Add(new ObservablePoint(xValues2[i], yValues2[i]));
            }

            // Создаем SeriesCollection и добавляем в нее обе ScatterSeries
            SeriesCollection = new SeriesCollection { scatterSeries1, scatterSeries2 };

            DataContext = this;
        }

        public SeriesCollection SeriesCollection { get; set; }
    }
}
