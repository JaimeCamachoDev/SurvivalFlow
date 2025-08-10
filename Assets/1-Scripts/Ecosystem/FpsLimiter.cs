using UnityEngine;

/// <summary>
/// Limita la tasa de refresco para estabilizar los FPS.
/// </summary>
public class FpsLimiter : MonoBehaviour
{
    [Range(1, 60)] public int targetFps = 60;

    void Awake()
    {
        QualitySettings.vSyncCount = 0; // Permite que Application.targetFrameRate funcione
        Application.targetFrameRate = targetFps;
    }
}
