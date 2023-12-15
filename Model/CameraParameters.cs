using OpenCvSharp;

namespace camera.Model
{
    public class CameraParameters
    {
        public Mat HomographyMatrix { get; set; }
        // Другие параметры камеры, если необходимо

        // Конструктор по умолчанию для сериализации
        public CameraParameters() { }

        public CameraParameters(Mat homographyMatrix)
        {
            HomographyMatrix = homographyMatrix;
        }
    }
}
