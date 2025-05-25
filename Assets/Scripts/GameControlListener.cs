using UnityEngine;
using WebSocketSharp;

public class GameControlListener : MonoBehaviour
{
    WebSocket ws;

    void Start()
    {
        // Создаем подключение к серверу WebSocket
        ws = new WebSocket("ws://localhost:8080/GameControl");

        // Обрабатываем полученные сообщения
        ws.OnMessage += (sender, e) =>
        {
            var message = e.Data;

            // Если сообщение - пауза, вызываем метод PauseGame
            if (message == "{\"action\":\"pause\"}")
            {
                PauseController.PauseGame();
            }
            // Если сообщение - продолжение, вызываем метод ResumeGame
            else if (message == "{\"action\":\"resume\"}")
            {
                PauseController.ResumeGame();
            }
        };

        // Подключаемся к серверу
        ws.Connect();
    }

    void OnApplicationQuit()
    {
        // Закрываем подключение при выходе из приложения
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Close();
        }
    }
}
