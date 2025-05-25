using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

public class DatasetCreator : MonoBehaviour
{
    [Header("Параметры захвата изображений")]
    public Camera captureCamera;
    public Transform[] cars;
    public int captureCount = 150;
    public float captureInterval = 0.1f; // Задержка между кадрами
    public string savePath = "Assets/CapturedImages";

    private int currentCount = 0;

    [Header("Параметры разбиения датасета")]
    [Range(0, 100)]
    public int trainPercentage = 80;
    [Range(0, 100)]
    public int valPercentage = 10;
    [Range(0, 100)]
    public int testPercentage = 10;

    public string destinationFolder = "Assets/YoloDataset";

    private Rect lastBoundingBox;
    private bool showBox = false;

    private void OnGUI()
    {
        if (showBox)
        {
            GUI.color = Color.red;
            GUI.DrawTexture(lastBoundingBox, Texture2D.whiteTexture);
        }
    }


    private void Start()
    {
        // Удаляем старые данные перед созданием нового датасета
        ClearOldDataset();

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        StartCoroutine(CaptureImages());

        cars = GameObject.FindGameObjectsWithTag("car")
                .Select(go => go.transform)
                .ToArray();

        // captureCamera.cullingMask = LayerMask.GetMask("Car");
    }

    private void ClearOldDataset()
    {
        if (Directory.Exists(savePath))
        {
            Directory.Delete(savePath, true); // Удаляет папку с изображениями и метками
        }
        if (Directory.Exists(destinationFolder))
        {
            Directory.Delete(destinationFolder, true); // Удаляет уже разделённый датасет
        }

        // Пересоздаем пустые папки
        Directory.CreateDirectory(savePath);
        Directory.CreateDirectory(destinationFolder);
    }

    private IEnumerator CaptureImages()
    {
        while (currentCount < captureCount)
        {
            yield return new WaitForEndOfFrame();

            string imgPath = Path.Combine(savePath, $"img_{currentCount}.jpg");
            CaptureCameraImage(imgPath);

            SaveLabel(currentCount);

            currentCount++;
            yield return new WaitForSeconds(captureInterval);
        }

        Debug.Log("✅ Захват изображений завершён. Начинаем разбиение датасета...");
        SplitDataset();
    }

    private void CaptureCameraImage(string path)
    {
        RenderTexture rt = new RenderTexture(640, 640, 24);
        captureCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(640, 640, TextureFormat.RGB24, false);

        captureCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
        screenShot.Apply();

        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenShot.EncodeToJPG(75); // 75% качество
        File.WriteAllBytes(path, bytes);

        Destroy(screenShot);
    }

    private void SaveLabel(int id)
    {
        string labelPath = Path.Combine(savePath, $"img_{id}.txt");
        HashSet<string> uniqueLabels = new HashSet<string>();
        List<string> lines = new List<string>();

        GameObject[] carObjects = GameObject.FindGameObjectsWithTag("car");

        foreach (GameObject car in carObjects)
        {
            MeshFilter[] meshFilters = car.GetComponentsInChildren<MeshFilter>();
            bool anyVisible = false;

            Vector2 min = new Vector2(1, 1);
            Vector2 max = new Vector2(0, 0);

            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                Vector3[] vertices = mf.sharedMesh.vertices;
                foreach (Vector3 vertex in vertices)
                {
                    Vector3 worldPoint = mf.transform.TransformPoint(vertex);
                    Vector3 screenPoint = captureCamera.WorldToViewportPoint(worldPoint);

                    if (screenPoint.z > 0)
                    {
                        anyVisible = true;
                        Vector2 point = new Vector2(screenPoint.x, screenPoint.y);
                        min = Vector2.Min(min, point);
                        max = Vector2.Max(max, point);
                    }
                }
            }

            if (!anyVisible) continue;

            min = Vector2.Max(min, Vector2.zero);
            max = Vector2.Min(max, Vector2.one);

            float centerX = (min.x + max.x) / 2f;
            float centerY = (min.y + max.y) / 2f;
            float width = max.x - min.x;
            float height = max.y - min.y;

            if (width > 0 && height > 0 && width <= 1 && height <= 1)
            {
                string line = string.Format(CultureInfo.InvariantCulture, "0 {0:F6} {1:F6} {2:F6} {3:F6}", centerX, centerY, width, height);

                if (!uniqueLabels.Contains(line))
                {
                    uniqueLabels.Add(line);
                    lines.Add(line);
                }
            }
        }

        if (lines.Count > 0)
        {
            File.WriteAllLines(labelPath, lines);
        }
    }

    private void SplitDataset()
    {
        if (!Directory.Exists(savePath))
        {
            Debug.LogError($"❌ Папка источника {savePath} не найдена!");
            return;
        }

        // Создаём папки
        Directory.CreateDirectory($"{destinationFolder}/images/train");
        Directory.CreateDirectory($"{destinationFolder}/images/val");
        Directory.CreateDirectory($"{destinationFolder}/images/test");

        Directory.CreateDirectory($"{destinationFolder}/labels/train");
        Directory.CreateDirectory($"{destinationFolder}/labels/val");
        Directory.CreateDirectory($"{destinationFolder}/labels/test");

        string[] imageFiles = Directory.GetFiles(savePath, "*.jpg");
        List<string> imageList = new List<string>(imageFiles);

        Shuffle(imageList);

        int total = imageList.Count;
        int trainCount = total * trainPercentage / 100;
        int valCount = total * valPercentage / 100;
        int testCount = total - trainCount - valCount;

        Debug.Log($"📦 Всего изображений: {total}, Train: {trainCount}, Val: {valCount}, Test: {testCount}");

        CopyFiles(imageList.GetRange(0, trainCount), "train");
        CopyFiles(imageList.GetRange(trainCount, valCount), "val");
        CopyFiles(imageList.GetRange(trainCount + valCount, testCount), "test");

        Debug.Log("✅ Разбиение датасета завершено!");
    }

    private void CopyFiles(List<string> files, string splitType)
    {
        foreach (string imagePath in files)
        {
            string fileName = Path.GetFileName(imagePath);
            string labelPath = Path.ChangeExtension(imagePath, ".txt");

            string destImagePath = $"{destinationFolder}/images/{splitType}/{fileName}";
            string destLabelPath = $"{destinationFolder}/labels/{splitType}/{Path.GetFileName(labelPath)}";

            File.Copy(imagePath, destImagePath, true);

            if (File.Exists(labelPath))
            {
                File.Copy(labelPath, destLabelPath, true);
            }
            else
            {
                Debug.LogWarning($"⚠️ Метка не найдена для изображения: {fileName}");
            }
        }
    }

    private void Shuffle(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            string temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
