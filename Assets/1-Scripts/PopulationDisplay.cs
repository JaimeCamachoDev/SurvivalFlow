using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra en pantalla las cantidades actuales de plantas, herbívoros y carnívoros.
/// Se actualiza cada cuadro para reflejar el estado del sistema.
/// </summary>
public class PopulationDisplay : MonoBehaviour
{
    public Text plantsText;
    public Text herbivoresText;
    public Text carnivoresText;

    void Update()
    {
        int plantCount = VegetationManager.Instance != null ?
            VegetationManager.Instance.activeVegetation.Count : 0;
        int herbCount = FindObjectsByType<Herbivore>(FindObjectsSortMode.None).Length;
        int carnCount = FindObjectsByType<Carnivore>(FindObjectsSortMode.None).Length;

        if (plantsText != null)
            plantsText.text = $"Plantas: {plantCount}";
        if (herbivoresText != null)
            herbivoresText.text = $"Herbívoros: {herbCount}";
        if (carnivoresText != null)
            carnivoresText.text = $"Carnívoros: {carnCount}";
    }
}
