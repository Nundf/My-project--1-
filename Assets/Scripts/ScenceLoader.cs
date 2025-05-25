using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SceneLoader
{
    [MenuItem("Python/Load Scene")]
    public static void LoadFromPython()
    {
        string scenePath = "Assets/CubexCube - Free City Pack I/Scenes/Free_City_Scene_I.unity";

        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogError("Сцена не найдена по пути: " + scenePath);
            return;
        }

        EditorSceneManager.OpenScene(scenePath);
        Debug.Log("Сцена загружена из Python.");

        EditorApplication.isPlaying = true; // <-- Запуск Play Mode
    }
}
