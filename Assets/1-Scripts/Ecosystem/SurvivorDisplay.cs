using TMPro;
using UnityEngine;

/// <summary>
/// Muestra los datos del herbívoro y carnívoro que más tiempo han vivido.
/// </summary>
public class SurvivorDisplay : MonoBehaviour
{
    public TextMeshProUGUI herbivoreText;
    public TextMeshProUGUI carnivoreText;

    void Update()
    {
        var h = SurvivorTracker.bestHerbivore;
        if (herbivoreText != null && h.lifespan > 0f)
            herbivoreText.text = $"Herbívoro top: {h.name} ({h.lifespan:F1}s)";

        var c = SurvivorTracker.bestCarnivore;
        if (carnivoreText != null && c.lifespan > 0f)
            carnivoreText.text = $"Carnívoro top: {c.name} ({c.lifespan:F1}s)";
    }
}
