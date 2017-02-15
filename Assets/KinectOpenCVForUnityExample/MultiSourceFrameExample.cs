using UnityEngine;
using System.Collections;
using Windows.Kinect;

using OpenCVForUnity;

public class MultiSourceFrameExample : MonoBehaviour
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
	public enum modeType
	{
		original,
		sepia,
		pixelize,
		comic
	}
	
	public modeType mode;
	Mat rgbaMat;
	Mat maskMat;
	Mat outputMat;


	//sepia
	Mat sepiaKernel;
	
	//pixelize
	Size pixelizeSize0;
	Mat pixelizeIntermediateMat;
	
	//comic
	Mat comicGrayMat;
	Mat comicLineMat;
	Mat comicMaskMat;
	Mat comicBgMat;
	Mat comicDstMat;
	byte[] comicGrayPixels;
	byte[] comicMaskPixels;
	
	void Start ()
	{
		sensor = KinectSensor.GetDefault ();
		
		if (sensor != null) {
			coordinateMapper = sensor.CoordinateMapper;

			reader = sensor.OpenMultiSourceFrameReader (FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex);
			
			FrameDescription colorFrameDesc = sensor.ColorFrameSource.CreateFrameDescription (ColorImageFormat.Rgba);
			texture = new Texture2D (colorFrameDesc.Width, colorFrameDesc.Height, TextureFormat.RGBA32, false);
			colorData = new byte[colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels];


			FrameDescription depthFrameDesc = sensor.DepthFrameSource.FrameDescription;
			depthData = new ushort[depthFrameDesc.LengthInPixels];
			depthSpacePoints = new DepthSpacePoint[colorFrameDesc.LengthInPixels];

			FrameDescription bodyIndexFrameDesc = sensor.BodyIndexFrameSource.FrameDescription;
			bodyIndexData = new byte[bodyIndexFrameDesc.BytesPerPixel * bodyIndexFrameDesc.LengthInPixels];
		
			
			if (!sensor.IsOpen) {
				sensor.Open ();
			}

			rgbaMat = new Mat (colorFrameDesc.Height, colorFrameDesc.Width, CvType.CV_8UC4);
			Debug.Log ("rgbaMat " + rgbaMat.ToString ());

			maskMat = new Mat (rgbaMat.rows (), rgbaMat.cols (), CvType.CV_8UC1);
			outputMat = new Mat (rgbaMat.rows (), rgbaMat.cols (), CvType.CV_8UC4);
			
			maskData = new byte[rgbaMat.rows () * rgbaMat.cols ()];
			
			gameObject.transform.localScale = new Vector3 (texture.width, texture.height, 1);
			gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
			Camera.main.orthographicSize = texture.height / 2;



			// sepia
			sepiaKernel = new Mat (4, 4, CvType.CV_32F);
			sepiaKernel.put (0, 0, /* R */0.189f, 0.769f, 0.393f, 0f);
			sepiaKernel.put (1, 0, /* G */0.168f, 0.686f, 0.349f, 0f);
			sepiaKernel.put (2, 0, /* B */0.131f, 0.534f, 0.272f, 0f);
			sepiaKernel.put (3, 0, /* A */0.000f, 0.000f, 0.000f, 1f);
			
			
			// pixelize
			pixelizeIntermediateMat = new Mat ();
			pixelizeSize0 = new Size ();
			
			
			//comic
			comicGrayMat = new Mat (texture.height, texture.width, CvType.CV_8UC1);
			comicLineMat = new Mat (texture.height, texture.width, CvType.CV_8UC1);
			comicMaskMat = new Mat (texture.height, texture.width, CvType.CV_8UC1);
			
			//create a striped background.
			comicBgMat = new Mat (texture.height, texture.width, CvType.CV_8UC1, new Scalar (255));
			for (int i = 0; i < comicBgMat.rows ()*2.5f; i=i+4) {
#if OPENCV_3
				Imgproc.line (comicBgMat, new Point (0, 0 + i), new Point (comicBgMat.cols (), -comicBgMat.cols () + i), new Scalar (0), 1);
#else
				Core.line (comicBgMat, new Point (0, 0 + i), new Point (comicBgMat.cols (), -comicBgMat.cols () + i), new Scalar (0), 1);
#endif
			}
			
			comicDstMat = new Mat (texture.height, texture.width, CvType.CV_8UC1);
			
			comicGrayPixels = new byte[comicGrayMat.cols () * comicGrayMat.rows () * comicGrayMat.channels ()];
			comicMaskPixels = new byte[comicMaskMat.cols () * comicMaskMat.rows () * comicMaskMat.channels ()];
		} else {
			UnityEngine.Debug.LogError ("No ready Kinect found!");
		}

	}
	
	void Update ()
	{
		if (reader != null) {
			MultiSourceFrame frame = reader.AcquireLatestFrame ();
			if (frame != null) {
			
				using (ColorFrame colorFrame = frame.ColorFrameReference.AcquireFrame()) {
					if (colorFrame != null) {
						colorFrame.CopyConvertedFrameDataToArray (colorData, ColorImageFormat.Rgba);
					}
				
				}
				using (DepthFrame depthFrame = frame.DepthFrameReference.AcquireFrame()) {
					if (depthFrame != null) {
						//Debug.Log ("bodyIndexFrame not null");
						depthFrame.CopyFrameDataToArray (depthData);
					}
				}
				using (BodyIndexFrame bodyIndexFrame = frame.BodyIndexFrameReference.AcquireFrame()) {
					if (bodyIndexFrame != null) {
						//Debug.Log ("bodyIndexFrame not null");
						bodyIndexFrame.CopyFrameDataToArray (bodyIndexData);
					}
				}

				frame = null;
			}
		} else {
			return;
		}

		Utils.copyToMat (colorData, outputMat);

		Utils.copyToMat (colorData, rgbaMat);

		coordinateMapper.MapColorFrameToDepthSpace (depthData, depthSpacePoints);
		int width = rgbaMat.width ();
		int height = rgbaMat.height ();
		int depthWidth = 512;
		byte[] maskOn = new byte[]{0};
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {

				int index = x + y * width;

				if (bodyIndexData [(int)depthSpacePoints [index].X + (int)depthSpacePoints [index].Y * depthWidth] == 255) {
					maskData [index] = 0;
				} else {
					maskData [index] = 255;
				}
			}
		}
		Utils.copyToMat (maskData, maskMat);

		
		if (mode == modeType.original) {

			rgbaMat.copyTo (outputMat, maskMat);

#if OPENCV_3
			Imgproc.putText (outputMat, "ORIGINAL MODE " + texture.width + "x" + texture.height, new Point (5, texture.height - 5), Core.FONT_HERSHEY_PLAIN, 4.0, new Scalar (255, 0, 0, 255), 3);	
#else
			Core.putText (outputMat, "ORIGINAL MODE " + texture.width + "x" + texture.height, new Point (5, texture.height - 5), Core.FONT_HERSHEY_PLAIN, 4.0, new Scalar (255, 0, 0, 255), 3);	
#endif
		} else if (mode == modeType.sepia) {
			
			Core.transform (rgbaMat, rgbaMat, sepiaKernel);

			rgbaMat.copyTo (outputMat, maskMat);

#if OPENCV_3
			Imgproc.putText (outputMat, "SEPIA MODE " + texture.width + "x" + texture.height, new Point (5, texture.height - 5), Core.FONT_HERSHEY_PLAIN, 4.0, new Scalar (255, 0, 0, 255), 3);
#else
			Core.putText (outputMat, "SEPIA MODE " + texture.width + "x" + texture.height, new Point (5, texture.height - 5), Core.FONT_HERSHEY_PLAIN, 4.0, new Scalar (255, 0, 0, 255), 3);
#endif
		} else if (mode == modeType.pixelize) {
			Imgproc.resize (rgbaMat, pixelizeIntermediateMat, pixelizeSize0, 0.1, 0.1, Imgproc.INTER_NEAREST);
			Imgproc.resize (pixelizeIntermediateMat, rgbaMat, rgbaMat.size (), 0.0, 0.0, Imgproc.INTER_NEAREST);

			rgbaMat.copyTo (outputMat, maskMat);

#if OPENCV_3
			Imgproc.putText (outputMat, "PIXELIZE MODE" + texture.width + "x" + texture.height, new Point (5, texture.height - 5), Core.FONT_HERSHEY_PLAIN, 4.0, new Scalar (255, 0, 0, 255), 3);
#else
			Core.putText (outputMat, "PIXELIZE MODE" + texture.width + "x" + texture.height, new Point (5, texture.height - 5), Core.FONT_HERSHEY_PLAIN, 4.0, new Scalar (255, 0, 0, 255), 3);
#endif
		} else if (mode == modeType.comic) {
			Imgproc.cvtColor (rgbaMat, comicGrayMat, Imgproc.COLOR_RGBA2GRAY);
			
			comicBgMat.copyTo (comicDstMat);
			
			Imgproc.GaussianBlur (comicGrayMat, comicLineMat, new Size (3, 3), 0);
			
			
			Utils.copyFromMat (comicGrayMat, comicGrayPixels);
			
			for (int i = 0; i < comicGrayPixels.Length; i++) {
				
				comicMaskPixels [i] = 0;
				
				if (comicGrayPixels [i] < 70) {
					comicGrayPixels [i] = 0;
					
					comicMaskPixels [i] = 1;
				} else if (70 <= comicGrayPixels [i] && comicGrayPixels [i] < 120) {
					comicGrayPixels [i] = 100;
					
					
				} else {
					comicGrayPixels [i] = 255;
					
					comicMaskPixels [i] = 1;
				}
			}
			
			
			Utils.copyToMat (comicGrayPixels, comicGrayMat);
			
			Utils.copyToMat (comicMaskPixels, comicMaskMat);
			
			comicGrayMat.copyTo (comicDstMat, comicMaskMat);
			
			
			Imgproc.Canny (comicLineMat, comicLineMat, 20, 120);
			
			comicLineMat.copyTo (comicMaskMat);
			
			Core.bitwise_not (comicLineMat, comicLineMat);
			
			comicLineMat.copyTo (comicDstMat, comicMaskMat);
			
			
			Imgproc.cvtColor (comicDstMat, rgbaMat, Imgproc.COLOR_GRAY2RGBA);


			rgbaMat.copyTo (outputMat, maskMat);

#if OPENCV_3
			Imgproc.putText (outputMat, "COMIC MODE " + texture.width + "x" + texture.height, new Point (5, texture.height - 5), Core.FONT_HERSHEY_PLAIN, 4.0, new Scalar (255, 0, 0, 255), 3);	
#else
			Core.putText (outputMat, "COMIC MODE " + texture.width + "x" + texture.height, new Point (5, texture.height - 5), Core.FONT_HERSHEY_PLAIN, 4.0, new Scalar (255, 0, 0, 255), 3);	
#endif
		}
		
		Utils.matToTexture (outputMat, texture);
	}
	
	void OnApplicationQuit ()
	{
		if (reader != null) {
			reader.Dispose ();
			reader = null;
		}
		
		if (sensor != null) {
			if (sensor.IsOpen) {
				sensor.Close ();
			}
			
			sensor = null;
		}
	}
	
	void OnGUI ()
	{
		float screenScale = Screen.width / 480.0f;
		Matrix4x4 scaledMatrix = Matrix4x4.Scale (new Vector3 (screenScale, screenScale, screenScale));
		GUI.matrix = scaledMatrix;
		
		
		GUILayout.BeginVertical ();
		
		if (GUILayout.Button ("original")) {
			mode = modeType.original;
		}
		
		if (GUILayout.Button ("sepia")) {
			mode = modeType.sepia;
		}
		
		if (GUILayout.Button ("pixelize")) {
			mode = modeType.pixelize;
		}

		if (GUILayout.Button ("comic")) {
			mode = modeType.comic;
		}
		
		GUILayout.EndVertical ();
	}
}

