using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Concurrent;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif



public class WebSocketServerScript1 : MonoBehaviour
{
    private WebSocketServer wss;
    private Texture2D texture;
    private Camera mainCamera;
    private RenderTexture renderTexture;
    private byte[] jpegData;

    public float moveSpeed = 5.0f;
    public float smoothTime = 0.01f;
    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;

    public static bool isCarDetected = false;
    public static Vector3 bestCarPosition = Vector3.zero;
    public static float bestCarConfidence = 0f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    public static readonly ConcurrentQueue<System.Action> mainThreadActions = new ConcurrentQueue<System.Action>();
    
    public static WebSocketServerScript1 Instance;

    void Start()
    {
        mainCamera = Camera.main;
        targetPosition = transform.position;
        renderTexture = new RenderTexture(1024, 1024, 16);
        mainCamera.targetTexture = renderTexture;
        texture = new Texture2D(1024, 1024, TextureFormat.RGB24, false);

        wss = new WebSocketServer("ws://localhost:8080");
        wss.AddWebSocketService<CameraStream1>("/CameraStream");
        wss.AddWebSocketService<CarStatusService>("/CarStatus");
        wss.AddWebSocketService<SceneControlService>("/SceneControl");
        wss.Start();

        initialPosition = transform.position;
        initialRotation = mainCamera.transform.rotation;

        captureCoroutine = StartCoroutine(CaptureCameraOutput());
    }

    
    void Update()
    {   
        HandleCarTracking();
        while (mainThreadActions.Count > 0)
        {
            mainThreadActions.TryDequeue(out var action);
            action?.Invoke();
        }
        HandleMovementInput();
        transform.position = Vector3.Lerp(transform.position, targetPosition, 0.1f);
    }

    private Coroutine captureCoroutine;

    public void PauseScene()
    {
        Time.timeScale = 0f;
        Debug.Log("Сцена поставлена на паузу");
    }

    public void ResumeScene()
    {   
        Time.timeScale = 1f;
        Debug.Log("Сцена продолжена");
    }

    public void StopScene()
    {
        if (captureCoroutine != null)
        {
            StopCoroutine(captureCoroutine);
            captureCoroutine = null;
        }

        mainCamera.enabled = false;
        Time.timeScale = 0f;
        enabled = false; // отключаем Update()
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

    private void RotateTowardsCarIfVisible()
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

        if (nearestCar != null && IsCarVisible(nearestCar.transform.position))
        {
            Vector3 direction = nearestCar.transform.position - transform.position;

            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
            }
        }
    }

    private void HandleCarTracking()
    {
        if (isCarDetected)
        {
            RotateTowardsCarIfVisible();
        }
        else
        {
            // Нет машины — вернуть на начальную позицию и поворот
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, initialRotation, Time.deltaTime * 2f);
            transform.position = Vector3.SmoothDamp(transform.position, initialPosition, ref velocity, smoothTime);
        }
    }


    private void OnApplicationQuit()
    {
        wss?.Stop();
        wss = null;

        if (texture != null) Destroy(texture);
        if (renderTexture != null) Destroy(renderTexture);
    }

    private bool IsCarVisible(Vector3 carPosition)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        Bounds bounds = new Bounds(carPosition, Vector3.one); // предполагаем размер машины ~ 1 куб

        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    void Awake()
    {
        Instance = this;
    }

    #if UNITY_EDITOR
    public static void StopPlayModeAndSaveScene()
    {
        Debug.Log("Остановка сцены и выход в редактор");
        EditorSceneManager.SaveOpenScenes();
        EditorApplication.isPlaying = false;
    }
    #endif
}

public class CameraStream1 : WebSocketBehavior
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

public class CarStatusService : WebSocketBehavior
{
    protected override void OnMessage(MessageEventArgs e)
    {
        string message = Encoding.UTF8.GetString(e.RawData);
        JObject json = JObject.Parse(message);

        bool detected = json.Value<bool>("car_detected");
        WebSocketServerScript1.isCarDetected = detected;

        if (detected)
        {
            float confidence = json.Value<float>("confidence");
            JArray posArray = json["position"] as JArray;

            Vector3 position = new Vector3(
                posArray[0].Value<float>(),
                posArray[1].Value<float>(),
                posArray[2].Value<float>()
            );

            WebSocketServerScript1.bestCarPosition = position;
            WebSocketServerScript1.bestCarConfidence = confidence;
        }
    }
}

public class SceneControlService : WebSocketBehavior
{
    protected override void OnMessage(MessageEventArgs e)
    {
        string msg = Encoding.UTF8.GetString(e.RawData);

        try
        {
            JObject json = JObject.Parse(msg);
            string action = json.Value<string>("action");

            if (action == "pause")
            {
                WebSocketServerScript1.mainThreadActions.Enqueue(() =>
                {
                    WebSocketServerScript1.Instance.PauseScene();
                });
            }
            else if (action == "resume")
            {
                WebSocketServerScript1.mainThreadActions.Enqueue(() =>
                {
                    WebSocketServerScript1.Instance.ResumeScene();
                });
            }
            else if (action == "stop")
            {
                WebSocketServerScript1.mainThreadActions.Enqueue(() =>
                {
            #if UNITY_EDITOR
                WebSocketServerScript1.StopPlayModeAndSaveScene();
            #else
                Time.timeScale = 1f;
                SceneManager.LoadScene("SampleScene");
            #endif
                });
            }

            else if (action == "add_object")
            {
                Debug.Log("[SceneControl] Получено сообщение add_object");

                string objectType = json.Value<string>("object_type");
                bool inFrontOfCamera = json.Value<bool?>("in_front_of_camera") ?? false;

                Vector3 position = Vector3.zero;

                if (json["screen_position"] is JArray screenArray && screenArray.Count == 2)
                {
                    float screenX = screenArray[0].Value<float>();
                    float screenY = screenArray[1].Value<float>();
                    Vector3 screenPos = new Vector3(screenX, screenY, 5f); // 5f — расстояние от камеры

                    WebSocketServerScript1.mainThreadActions.Enqueue(() =>
                    {
                        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                        CreateObject(objectType, worldPos);
                    });
                }
                else
                {
                    if (inFrontOfCamera && Camera.main != null)
                    {
                        position = Camera.main.transform.position + Camera.main.transform.forward * 5f;
                    }   
                    else if (json["position"] is JArray posArray && posArray.Count == 3)
                    {
                        position = new Vector3(
                            posArray[0].Value<float>(),
                            posArray[1].Value<float>(),
                            posArray[2].Value<float>()
                        );
                    }
                    else
                    {
                        Debug.LogError("[SceneControl] Ошибка: неверный формат команды add_object");
                        return;
                    }

                    WebSocketServerScript1.mainThreadActions.Enqueue(() =>
                    {
                        CreateObject(objectType, position);
                    });
                }
            }

            else if (action == "update_position")
            {
                string name = json.Value<string>("object_name");
                JArray newPos = json["new_position"] as JArray;

                Vector3 newPosition = new Vector3(
                    newPos[0].Value<float>(),
                    newPos[1].Value<float>(),
                    newPos[2].Value<float>()
                    );

                WebSocketServerScript1.mainThreadActions.Enqueue(() =>
                {
                    GameObject obj = GameObject.Find(name);
                    if (obj != null)
                        obj.transform.position = newPosition;
                    else
                        Debug.LogWarning($"[SceneControl] Object '{name}' not found.");
                });
            }
            else if (action == "remove_object")
            {
                string name = json.Value<string>("object_name");

                WebSocketServerScript1.mainThreadActions.Enqueue(() =>
                {
                    GameObject obj = GameObject.Find(name);
                    if (obj != null)
                        GameObject.Destroy(obj);
                    else
                        Debug.LogWarning($"[SceneControl] Object '{name}' not found.");
                });
            }

            else if (action == "start_dataset")
            {
                Debug.Log("Received: start_dataset");

                MainThreadDispatcher.Enqueue(() =>
                {
                    GameObject cam = GameObject.Find("Camera1");
                    var controller = cam.GetComponent<CameraModeController>();
                    if (controller != null)
                    {
                        controller.EnableDatasetMode();
                    }
                });
            }
            else if (action == "start_main")
            {
                Debug.Log("Received: start_main");

                MainThreadDispatcher.Enqueue(() =>
                {
                    GameObject cam = GameObject.Find("Camera1");
                    var controller = cam.GetComponent<CameraModeController>();
                    if (controller != null)
                    {
                        controller.EnableMainMode();
                    }
                });
            }

            else
            {
                Debug.LogWarning($"Неизвестная команда: {action}");
            }
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SceneControl] Ошибка при обработке команды: " + ex.Message);
            // Debug.Log("[SceneControl] Получено сообщение: " + msg);
        }

        
    }

    private void CreateObject(string objectType, Vector3 position)
    {
        GameObject obj = null;

        if (objectType == "Cube")
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        else if (objectType == "Sphere")
            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        else
        {
            Debug.LogWarning($"[SceneControl] Неизвестный тип объекта: {objectType}");
        }

        if (obj != null)
        {
            obj.transform.position = position;
            string uniqueName = objectType + "_" + Random.Range(1000, 9999);
            obj.name = uniqueName;

            Debug.Log($"[SceneControl] Объект {uniqueName} добавлен в позицию: {position}");

            JObject response = new JObject
            {
                ["action"] = "object_added",
                ["object_name"] = uniqueName,
                ["position"] = new JArray(position.x, position.y, position.z)
            };

            Sessions.Broadcast(Encoding.UTF8.GetBytes(response.ToString()));

    #if UNITY_EDITOR
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    #endif
        }
    }

}

