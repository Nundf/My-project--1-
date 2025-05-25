using UnityEngine;
using UnityEngine.Splines;

public class MoveOnSpline : MonoBehaviour
{
    public SplineContainer splineContainer;
    public float speed = 5f; // Скорость движения
    private float t = 0f;    // Параметр от 0 до 1 (позиция на кривой)
    public GameObject carRoot;
    void Update()
    {
        if (splineContainer == null) return;

        t += speed * Time.deltaTime / splineContainer.CalculateLength();
        t %= 1f; // Зацикливаем

        var eval = splineContainer.EvaluatePosition(t);
        carRoot.transform.position = eval;

        var forward = splineContainer.EvaluateTangent(t);
        carRoot.transform.rotation = Quaternion.LookRotation(forward);

    }
}
