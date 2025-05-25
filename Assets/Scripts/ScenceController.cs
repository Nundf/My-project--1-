using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static void PauseScene()
    {
        Time.timeScale = 0;
        Debug.Log("Сцена поставлена на паузу");
    }

    public static void ResumeScene()
    {
        Time.timeScale = 1;
        Debug.Log("Сцена возобновлена");
    }

    public static void UnloadSceneFromPython()
    {
        string sceneName = "Free_City_Scene_I"; // или как у тебя точно называется
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            SceneManager.UnloadSceneAsync(sceneName);
            Debug.Log("Сцена выгружена");
        }
        else
        {
            Debug.Log("Сцена не загружена");
        }
    }
}
