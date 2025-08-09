using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Permite ajustar parámetros clave del ecosistema en tiempo real desde un panel.
/// </summary>
public class EcosystemSettingsUI : MonoBehaviour
{
    [Header("Referencias")]
    public VegetationManager vegetationManager;
    public PopulationBalancer populationBalancer;

    [Header("UI")]
    public Slider plantReproductionSlider;
    public TextMeshProUGUI plantReproductionText;
    public Slider herbivoreMinSlider;
    public TextMeshProUGUI herbivoreMinText;
    public Slider carnivoreMinSlider;
    public TextMeshProUGUI carnivoreMinText;

    void Start()
    {
        if (vegetationManager != null && plantReproductionSlider != null)
        {
            plantReproductionSlider.minValue = 1f;
            plantReproductionSlider.maxValue = 60f;
            plantReproductionSlider.value = vegetationManager.reproductionInterval;
            plantReproductionSlider.onValueChanged.AddListener(v =>
            {
                vegetationManager.reproductionInterval = v;
                UpdateTexts();
            });
        }

        if (populationBalancer != null)
        {
            if (herbivoreMinSlider != null)
            {
                herbivoreMinSlider.minValue = 0;
                herbivoreMinSlider.maxValue = 100;
                herbivoreMinSlider.value = populationBalancer.minHerbivores;
                herbivoreMinSlider.onValueChanged.AddListener(v =>
                {
                    populationBalancer.minHerbivores = Mathf.RoundToInt(v);
                    UpdateTexts();
                });
            }

            if (carnivoreMinSlider != null)
            {
                carnivoreMinSlider.minValue = 0;
                carnivoreMinSlider.maxValue = 100;
                carnivoreMinSlider.value = populationBalancer.minCarnivores;
                carnivoreMinSlider.onValueChanged.AddListener(v =>
                {
                    populationBalancer.minCarnivores = Mathf.RoundToInt(v);
                    UpdateTexts();
                });
            }
        }

        UpdateTexts();
    }

    void UpdateTexts()
    {
        if (plantReproductionText != null && vegetationManager != null)
            plantReproductionText.text = $"Reproducción plantas: {vegetationManager.reproductionInterval:0.0}s";

        if (herbivoreMinText != null && populationBalancer != null)
            herbivoreMinText.text = $"Herbívoros mínimos: {populationBalancer.minHerbivores}";

        if (carnivoreMinText != null && populationBalancer != null)
            carnivoreMinText.text = $"Carnívoros mínimos: {populationBalancer.minCarnivores}";
    }
}
