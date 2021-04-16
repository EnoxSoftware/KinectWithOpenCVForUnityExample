# Kinect With OpenCVForUnity Example
- An example of reading color frame data from Kinect and adding image processing.
- An example of reading multiple source frame data from Kinect and applying image processing only to the human body area.

## Demo Video
[![](http://img.youtube.com/vi/_dvsSo8rzA8/0.jpg)](https://www.youtube.com/watch?v=_dvsSo8rzA8)

## Environment
- Windows 10
- Kinect for Xbox One ("Kinect v2") + Kinect Adapter for Windows
- Unity >= 2018.4.28f1+
- [OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088?aid=1011l4ehR) 2.4.3+
- Kinect.2.0.1410.19000.unitypackage contained in [KinectForWindows_UnityPro_2.0.1410.zip](https://go.microsoft.com/fwlink/p/?LinkId=513177)

## Setup
1. Setup "Kinect v2" device. (See [Kinect for Windows](https://developer.microsoft.com/en-us/windows/kinect/))
1. Download the latest release unitypackage. [KinectWithOpenCVForUnityExample.unitypackage](https://github.com/EnoxSoftware/KinectWithOpenCVForUnityExample/releases)
1. Create a new project. (KinectWithOpenCVForUnityExample)
1. Import Kinect.2.0.1410.19000.unitypackage 
1. Import OpenCVForUnity.
    * Setup the OpenCVForUnity. (Tools > OpenCV for Unity > Set Plugin Import Settings)
1. Import the KinectWithOpenCVForUnityExample.unitypackage.
1. Add the "Assets/KinectWithOpenCVForUnityExample/*.unity" files to the "Scenes In Build" list in the "Build Settings" window.


## Examples
**[KinectColorFrameExample.cs](/Assets/KinectWithOpenCVForUnityExample/KinectColorFrameExample/KinectColorFrameExample.cs)**  
Converts ColorFrame acquired from "Kinect" to Mat of "OpenCV", perform image processing.

**[KinectMultiSourceFrameExample.cs](/Assets/KinectWithOpenCVForUnityExample/KinectMultiSourceFrameExample/KinectMultiSourceFrameExample.cs)**  
Converts BodyIndexFrame acquired from "Kinect" to Mat of "OpenCV", perform image processing only person.
