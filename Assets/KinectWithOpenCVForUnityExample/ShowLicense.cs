using UnityEngine;
using UnityEngine.SceneManagement;

namespace KinectWithOpenCVForUnityExample
{

    public class ShowLicense : MonoBehaviour
    {

        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("KinectWithOpenCVForUnityExample");
        }
    }
}
