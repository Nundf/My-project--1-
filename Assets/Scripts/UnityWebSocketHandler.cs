using UnityEngine;
using WebSocketSharp;
using System.Collections.Generic;
using System.Collections;

public class UnityWebSocketHandler : MonoBehaviour
{
    private WebSocket ws;
    private GameObject selectedObject;
    private Dictionary<string, GameObject> objectPrefabs;
    
    public Transform spawnPoint;  // Точка появления объектов
    public GameObject cubePrefab;
    public GameObject spherePrefab;
    public GameObject trianglePrefab;

    void Start()
    {
        // Инициализация WebSocket и подключение
        ws = new WebSocket("ws://localhost:8080/CommandStream");
        ws.OnMessage += OnMessageReceived;
        ws.Connect();

        // Инициализация префабов объектов
        objectPrefabs = new Dictionary<string, GameObject>
        {
            { "cube", cubePrefab },
            { "sphere", spherePrefab },
            { "triangle", trianglePrefab }
        };
    }

    void OnMessageReceived(object sender, MessageEventArgs e)
    {
        string message = e.Data;

        if (message.Contains("car_detected"))
        {
            Debug.Log("Машина обнаружена!");
            // Можешь добавить логику для активации камеры или создания объектов
        }
        else if (message.Contains("no_car"))
        {
            Debug.Log("Машина не обнаружена!");
            // Можешь добавить логику для деактивации камеры или удаления объектов
        }
        else
        {
            HandleObjectCommand(message);
        }
    }

    void HandleObjectCommand(string command)
    {
        // Пример команды: "add|cube|0,0,0" - добавить куб по координатам (0,0,0)
        string[] parts = command.Split('|');

        if (parts.Length == 3 && parts[0] == "add")
        {
            string objectType = parts[1];
            string[] coords = parts[2].Split(',');

            if (objectPrefabs.ContainsKey(objectType))
            {
                Vector3 position = new Vector3(float.Parse(coords[0]), float.Parse(coords[1]), float.Parse(coords[2]));
                Instantiate(objectPrefabs[objectType], position, Quaternion.identity);
            }
        }
        else if (parts.Length == 2 && parts[0] == "remove")
        {
            string objectName = parts[1];
            GameObject objectToRemove = GameObject.Find(objectName);
            if (objectToRemove != null)
            {
                Destroy(objectToRemove);
            }
        }
        // Добавь обработку других команд (например, для обновления объектов)
    }

    void OnApplicationQuit()
    {
        // Закрытие соединения при выходе
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Close();
        }
    }
}
