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
    /// Kinect Color Frame Example
    /// An example of reading color frame data from Kinect and adding image processing.
    /// </summary>
    public class KinectColorFrameExample : MonoBehaviour
    {
        KinectSensor sensor;
        ColorFrameReader reader;
        Texture2D texture;
        byte[] data;
        Mat rgbaMat;

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
                reader = sensor.ColorFrameSource.OpenReader();

                FrameDescription frameDesc = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);


                texture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.RGBA32, false);
                data = new byte[frameDesc.BytesPerPixel * frameDesc.LengthInPixels];

                if (!sensor.IsOpen)
                {
                    sensor.Open();
                }


                rgbaMat = new Mat(texture.height, texture.width, CvType.CV_8UC4);

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
                ColorFrame frame = reader.AcquireLatestFrame();

                if (frame != null)
                {
                    frame.CopyConvertedFrameDataToArray(data, ColorImageFormat.Rgba);

                    frame.Dispose();
                    frame = null;
                }
            }
            else
            {
                return;
            }

            MatUtils.copyToMat(data, rgbaMat);


            if (filterType == FilterTypePreset.NONE)
            {

                Imgproc.putText(rgbaMat, "Filter Type: NONE " + texture.width + "x" + texture.height, new Point(5, texture.height - 5), Imgproc.FONT_HERSHEY_PLAIN, 4.0, new Scalar(255, 0, 0, 255), 3);

            }
            else if (filterType == FilterTypePreset.SEPIA)
            {

                Core.transform(rgbaMat, rgbaMat, sepiaKernel);

                Imgproc.putText(rgbaMat, "Filter Type: SEPIA " + texture.width + "x" + texture.height, new Point(5, texture.height - 5), Imgproc.FONT_HERSHEY_PLAIN, 4.0, new Scalar(255, 0, 0, 255), 3);

            }
            else if (filterType == FilterTypePreset.PIXELIZE)
            {

                Imgproc.resize(rgbaMat, pixelizeIntermediateMat, pixelizeSize0, 0.1, 0.1, Imgproc.INTER_NEAREST);
                Imgproc.resize(pixelizeIntermediateMat, rgbaMat, rgbaMat.size(), 0.0, 0.0, Imgproc.INTER_NEAREST);

                Imgproc.putText(rgbaMat, "Filter Type: PIXELIZE " + texture.width + "x" + texture.height, new Point(5, texture.height - 5), Imgproc.FONT_HERSHEY_PLAIN, 4.0, new Scalar(255, 0, 0, 255), 3);

            }
            else if (filterType == FilterTypePreset.COMIC)
            {

                comicFilter.Process(rgbaMat, rgbaMat);

                Imgproc.putText(rgbaMat, "Filter Type: COMIC " + texture.width + "x" + texture.height, new Point(5, texture.height - 5), Imgproc.FONT_HERSHEY_PLAIN, 4.0, new Scalar(255, 0, 0, 255), 3);

            }

            Utils.matToTexture2D(rgbaMat, texture);
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