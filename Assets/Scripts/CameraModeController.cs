using UnityEngine;

public class CameraModeController : MonoBehaviour
{
    public MonoBehaviour mainCameraScript;
    public MonoBehaviour datasetCreatorScript;
    public MonoBehaviour labelGeneratorScript;

    public void EnableMainMode()
    {
        mainCameraScript.enabled = true;
        datasetCreatorScript.enabled = false;
        labelGeneratorScript.enabled = false;

        Debug.Log("Switched to MAIN mode.");
    }

    public void EnableDatasetMode()
    {
        mainCameraScript.enabled = false;
        datasetCreatorScript.enabled = true;
        labelGeneratorScript.enabled = true;

        Debug.Log("Switched to DATASET mode.");
    }
}
