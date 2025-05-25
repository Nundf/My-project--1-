using UnityEngine;

public class PauseController : MonoBehaviour
{
    public static bool isPaused = false;

    // Метод для приостановки игры
    public static void PauseGame()
    {
        Time.timeScale = 0;  // Приостанавливает игру
        isPaused = true;
    }

    // Метод для продолжения игры
    public static void ResumeGame()
    {
        Time.timeScale = 1;  // Возвращает нормальную скорость
        isPaused = false;
    }
}
