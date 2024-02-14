using OpenCvSharp;

namespace camera.Model
{
    public class CameraParametersModel
    {
        private Mat _homographyMatrix;

        public CameraParametersModel(Mat homographyMatrix)
        {
            _homographyMatrix = homographyMatrix;
        }
    }
}
