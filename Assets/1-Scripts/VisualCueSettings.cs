using UnityEngine;

/// <summary>
/// Control global para activar o desactivar señales visuales de depuración.
/// </summary>
public class VisualCueSettings : MonoBehaviour
{
    public static VisualCueSettings Instance;     // Referencia global para colores
    public static bool enableVisualCues = true;   // Acceso global

    [Header("General")]
    public bool showVisualCues = true;            // Ajuste desde el inspector

    [Header("Vegetación")]
    public Color plantGrowingColor = Color.yellow;
    public Color plantMatureColor = Color.green;
    public Color plantConsumedColor = Color.red;

    [Header("Herbívoros")]
    public Color herbivoreEatingColor = Color.green;
    public Color herbivoreFleeingColor = Color.red;
    public Color herbivoreReproducingColor = Color.cyan;
    public Color herbivoreInjuredColor = Color.magenta;

    [Header("Carnívoros")]
    public Color carnivoreEatingColor = Color.green;
    public Color carnivoreFleeingColor = Color.red;
    public Color carnivorePursuingColor = Color.yellow;
    public Color carnivoreReproducingColor = Color.cyan;
    public Color carnivoreInjuredColor = Color.magenta;

    void Awake()
    {
        Instance = this;
        enableVisualCues = showVisualCues;
    }

    void OnValidate()
    {
        enableVisualCues = showVisualCues;
    }
}
