using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Windows.Kinect;

namespace KinectWithOpenCVForUnityExample
{
    /// <summary>
    /// Kinect Multi Source Frame Example
    /// An example of reading multiple source frame data from Kinect and applying image processing only to the human body area.
    /// </summary>
    public class KinectMultiSourceFrameExample : MonoBehaviour
    {
        KinectSensor sensor;
        MultiSourceFrameReader reader;
        CoordinateMapper coordinateMapper;
        DepthSpacePoint[] depthSpacePoints;
        Texture2D texture;
        byte[] colorData;
        ushort[] depthData;
        byte[] bodyIndexData;
        byte[] maskData;

        Mat rgbaMat;
        Mat maskMat;
        Mat outputMat;

        int colorFrameWidth;
        int colorFrameHeight;
        int depthFrameWidth;
        int depthFrameHeight;

        public FilterTypePreset filterType = FilterTypePreset.NONE;
        public Dropdown filterTypeDropdown;


        //sepia
        Mat sepiaKernel;

        //pixelize
        Size pixelizeSize0;
        Mat pixelizeIntermediateMat;

        //comic
        ComicFilter comicFilter;

        void Start()
        {
            sensor = KinectSensor.GetDefault();

            if (sensor != null)
            {
                coordinateMapper = sensor.CoordinateMapper;

                reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex);

                FrameDescription colorFrameDesc = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
                texture = new Texture2D(colorFrameDesc.Width, colorFrameDesc.Height, TextureFormat.RGBA32, false);
                colorData = new byte[colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels];
                colorFrameWidth = colorFrameDesc.Width;
                colorFrameHeight = colorFrameDesc.Height;

                FrameDescription depthFrameDesc = sensor.DepthFrameSource.FrameDescription;
                depthData = new ushort[depthFrameDesc.LengthInPixels];
                depthSpacePoints = new DepthSpacePoint[colorFrameDesc.LengthInPixels];
                depthFrameWidth = depthFrameDesc.Width;
                depthFrameHeight = depthFrameDesc.Height;

                FrameDescription bodyIndexFrameDesc = sensor.BodyIndexFrameSource.FrameDescription;
                bodyIndexData = new byte[bodyIndexFrameDesc.BytesPerPixel * bodyIndexFrameDesc.LengthInPixels];


                if (!sensor.IsOpen)
                {
                    sensor.Open();
                }

                rgbaMat = new Mat(colorFrameDesc.Height, colorFrameDesc.Width, CvType.CV_8UC4);

                maskMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);
                outputMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC4);

                maskData = new byte[rgbaMat.rows() * rgbaMat.cols()];

                gameObject.transform.localScale = new Vector3(texture.width, texture.height, 1);
                gameObject.GetComponent<Renderer>().material.mainTexture = texture;

                float width = rgbaMat.width();
                float height = rgbaMat.height();

                float widthScale = (float)Screen.width / width;
                float heightScale = (float)Screen.height / height;
                if (widthScale < heightScale)
                {
                    Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                }
                else
                {
                    Camera.main.orthographicSize = height / 2;
                }



                // sepia
                sepiaKernel = new Mat(4, 4, CvType.CV_32F);
                sepiaKernel.put(0, 0, /* R */0.189f, 0.769f, 0.393f, 0f);
                sepiaKernel.put(1, 0, /* G */0.168f, 0.686f, 0.349f, 0f);
                sepiaKernel.put(2, 0, /* B */0.131f, 0.534f, 0.272f, 0f);
                sepiaKernel.put(3, 0, /* A */0.000f, 0.000f, 0.000f, 1f);


                // pixelize
                pixelizeIntermediateMat = new Mat();
                pixelizeSize0 = new Size();


                //comic
                comicFilter = new ComicFilter();
            }
            else
            {
                UnityEngine.Debug.LogError("No ready Kinect found!");
            }

            // Update GUI state
            filterTypeDropdown.value = (int)filterType;
        }

        void Update()
        {
            if (reader != null)
            {
                MultiSourceFrame frame = reader.AcquireLatestFrame();
                if (frame != null)
                {
                    using (ColorFrame colorFrame = frame.ColorFrameReference.AcquireFrame())
                    {
                        if (colorFrame != null)
                        {
                            colorFrame.CopyConvertedFrameDataToArray(colorData, ColorImageFormat.Rgba);
                        }

                    }
                    using (DepthFrame depthFrame = frame.DepthFrameReference.AcquireFrame())
                    {
                        if (depthFrame != null)
                        {
                            //Debug.Log ("bodyIndexFrame not null");
                            depthFrame.CopyFrameDataToArray(depthData);
                        }
                    }
                    using (BodyIndexFrame bodyIndexFrame = frame.BodyIndexFrameReference.AcquireFrame())
                    {
                        if (bodyIndexFrame != null)
                        {
                            //Debug.Log ("bodyIndexFrame not null");
                            bodyIndexFrame.CopyFrameDataToArray(bodyIndexData);
                        }
                    }

                    frame = null;
                }
            }
            else
            {
                return;
            }

            MatUtils.copyToMat(colorData, outputMat);
            MatUtils.copyToMat(colorData, rgbaMat);


            // update mask image from bodyIndexData.
            coordinateMapper.MapColorFrameToDepthSpace(depthData, depthSpacePoints);

            for (int colorY = 0; colorY < colorFrameHeight; colorY++)
            {
                for (int colorX = 0; colorX < colorFrameWidth; colorX++)
                {
                    int colorIndex = colorY * colorFrameWidth + colorX;
                    int depthX = (int)(depthSpacePoints[colorIndex].X);
                    int depthY = (int)(depthSpacePoints[colorIndex].Y);
                    if ((0 <= depthX) && (depthX < depthFrameWidth) && (0 <= depthY) && (depthY < depthFrameHeight))
                    {
                        int depthIndex = depthY * depthFrameWidth + depthX;

                        if (bodyIndexData[depthIndex] == 255)
                        {
                            maskData[colorIndex] = 0;
                        }
                        else
                        {
                            maskData[colorIndex] = 255;
                        }
                    }
                }
            }
            MatUtils.copyToMat(maskData, maskMat);


            if (filterType == FilterTypePreset.NONE)
            {

                rgbaMat.copyTo(outputMat, maskMat);

                Imgproc.putText(outputMat, "Filter Type: NONE " + texture.width + "x" + texture.height, new Point(5, texture.height - 5), Imgproc.FONT_HERSHEY_PLAIN, 4.0, new Scalar(255, 0, 0, 255), 3);

            }
            else if (filterType == FilterTypePreset.SEPIA)
            {

                Core.transform(rgbaMat, rgbaMat, sepiaKernel);
                rgbaMat.copyTo(outputMat, maskMat);

                Imgproc.putText(outputMat, "Filter Type: SEPIA " + texture.width + "x" + texture.height, new Point(5, texture.height - 5), Imgproc.FONT_HERSHEY_PLAIN, 4.0, new Scalar(255, 0, 0, 255), 3);

            }
            else if (filterType == FilterTypePreset.PIXELIZE)
            {

                Imgproc.resize(rgbaMat, pixelizeIntermediateMat, pixelizeSize0, 0.1, 0.1, Imgproc.INTER_NEAREST);
                Imgproc.resize(pixelizeIntermediateMat, rgbaMat, rgbaMat.size(), 0.0, 0.0, Imgproc.INTER_NEAREST);

                rgbaMat.copyTo(outputMat, maskMat);

                Imgproc.putText(outputMat, "Filter Type: PIXELIZE " + texture.width + "x" + texture.height, new Point(5, texture.height - 5), Imgproc.FONT_HERSHEY_PLAIN, 4.0, new Scalar(255, 0, 0, 255), 3);

            }
            else if (filterType == FilterTypePreset.COMIC)
            {

                comicFilter.Process(rgbaMat, rgbaMat);
                rgbaMat.copyTo(outputMat, maskMat);

                Imgproc.putText(outputMat, "Filter Type: COMIC " + texture.width + "x" + texture.height, new Point(5, texture.height - 5), Imgproc.FONT_HERSHEY_PLAIN, 4.0, new Scalar(255, 0, 0, 255), 3);

            }

            Utils.matToTexture2D(outputMat, texture);
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }

            if (sensor != null)
            {
                if (sensor.IsOpen)
                {
                    sensor.Close();
                }

                sensor = null;
            }

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
            if (rgbaMat != null)
            {
                rgbaMat.Dispose();
                rgbaMat = null;
            }
            if (outputMat != null)
            {
                outputMat.Dispose();
                outputMat = null;
            }
            if (maskMat != null)
            {
                maskMat.Dispose();
                maskMat = null;
            }
            if (comicFilter != null)
            {
                comicFilter.Dispose();
                comicFilter = null;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("KinectWithOpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the filter type dropdown value changed event.
        /// </summary>
        public void OnFilterTypeDropdownValueChanged(int result)
        {
            if ((int)filterType != result)
            {
                filterType = (FilterTypePreset)result;
            }
        }

        public enum FilterTypePreset
        {
            NONE = 0,
            SEPIA,
            PIXELIZE,
            COMIC
        }
    }
}