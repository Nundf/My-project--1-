using System.Collections;
using UnityEngine.Rendering;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class WebSocketServerScript : MonoBehaviour
{
    private WebSocketServer wss;
    private Texture2D texture;
    private Camera mainCamera;
    private RenderTexture renderTexture;
    private byte[] jpegData;

    public float moveSpeed = 5.0f; // Базовая скорость движения
    public float smoothTime = 0.1f; // Время сглаживания
    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;

    public Transform carTransform; // 🚗 Сюда присваиваем объект машины
    // public Vector3 cameraOffset = new Vector3(0, 5, -10); // Смещение камеры относительно машины
    public float orbitRadius = 10f;
    public float orbitSpeed = 20f;
    public float heightAmplitude = 2f;
    public float heightFrequency = 1f;
    private float orbitHeight = 5f;
    private float orbitAngle = 0f;


    void Start()
    {
        mainCamera = Camera.main;
        targetPosition = transform.position;

        renderTexture = new RenderTexture(512, 512, 16);
        mainCamera.targetTexture = renderTexture;

        texture = new Texture2D(512, 512, TextureFormat.RGB24, false);

        wss = new WebSocketServer("ws://localhost:8080");
        wss.AddWebSocketService<CameraStream1>("/CameraStream");
        wss.Start();

        // Можно автоматически найти машину по тегу "Car"
        if (carTransform == null)
        {
            GameObject carObject = GameObject.FindGameObjectWithTag("car");
            if (carObject != null)
                carTransform = carObject.transform;
        }

        StartCoroutine(CaptureCameraOutput());
    }

    void Update()
    {
        HandleMovementInput();
        // MoveCameraParallelToCar(); // Движение камеры параллельно машине

        //RotateTowardsCar(); // 🔄 Добавлено поворачивание к машине
        if (carTransform != null)
        {
            OrbitAroundCar();
            RotateTowardsCar(); // 🔄 Добавлено поворачивание к машине
        }
    }

    private void OrbitAroundCar()
    {
        orbitAngle += orbitSpeed * Time.deltaTime;
        float radians = orbitAngle * Mathf.Deg2Rad;

        float x = Mathf.Cos(radians) * orbitRadius;
        float z = Mathf.Sin(radians) * orbitRadius;
        float y = orbitHeight;

        Vector3 orbitPosition = carTransform.position + new Vector3(x, y, z);
        transform.position = orbitPosition;
    }


    // private void MoveCameraParallelToCar()
    // {
    //     if (carTransform != null)
    //     {
    //         // Вычисляем новую позицию камеры с учетом смещения относительно машины
    //         Vector3 desiredPosition = carTransform.position + cameraOffset;

    //         // Плавно двигаем камеру к этой позиции
    //         transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    //     }
    // }

    private void RotateTowardsCar()
    {
        GameObject[] cars = GameObject.FindGameObjectsWithTag("car");

        if (cars.Length == 0)
            return;

        GameObject nearestCar = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject car in cars)
        {
            float distance = Vector3.Distance(transform.position, car.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestCar = car;
            }
        }

        if (nearestCar != null)
        {
            Vector3 direction = nearestCar.transform.position - transform.position;

            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
            }
        }
    }

    private IEnumerator CaptureCameraOutput()
    {
        while (true)
        {
            if (renderTexture == null || mainCamera == null)
            {
                yield return null;
                continue;
            }

            AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, OnCompleteReadback);
            yield return new WaitForSeconds(0.05f); // 20 FPS
        }
    }

    private void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError || texture == null) return;
        var rewData = request.GetData<byte>();
        texture.LoadRawTextureData(rewData);
        texture.Apply();

        jpegData = texture.EncodeToJPG(50);
        var cameraStream = wss.WebSocketServices["/CameraStream"].Sessions;
        cameraStream.Broadcast(jpegData);
    }

    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float upDown = 0;

        if (Input.GetKey(KeyCode.Space)) upDown = 1;
        else if (Input.GetKey(KeyCode.LeftControl)) upDown = -1;

        Vector3 moveDirection = new Vector3(horizontal, upDown, vertical).normalized;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            targetPosition += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    private void OnApplicationQuit()
    {
        wss?.Stop();
        wss = null;

        if (texture != null)
        {
            Destroy(texture);
            texture = null;
        }

        if (renderTexture != null)
        {
            Destroy(renderTexture);
            renderTexture = null;
        }
    }
}

public class CameraStream : WebSocketBehavior
{
    protected override void OnMessage(MessageEventArgs e)
    {
        SendDataToClient(e.RawData);
    }

    public void SendDataToClient(byte[] data)
    {
        Sessions.Broadcast(data);
    }
}