using UnityEngine;

/// <summary>
/// Control global para activar o desactivar señales visuales de depuración.
/// </summary>
public class VisualCueSettings : MonoBehaviour
{
    public static bool enableVisualCues = true;    // Acceso global
    public bool showVisualCues = true;            // Ajuste desde el inspector

    void Awake()
    {
        enableVisualCues = showVisualCues;
    }

    void OnValidate()
    {
        enableVisualCues = showVisualCues;
    }
}
