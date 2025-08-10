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
    public GeneticManager geneticManager;

    [Header("UI")]
    public Slider plantReproductionSlider;
    public TextMeshProUGUI plantReproductionText;
    public Slider herbivoreMinSlider;
    public TextMeshProUGUI herbivoreMinText;
    public Slider carnivoreMinSlider;
    public TextMeshProUGUI carnivoreMinText;
    public Toggle managerToggle;
    public Toggle geneticsToggle;
    public Slider mutationSlider;
    public TextMeshProUGUI mutationText;

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

            if (managerToggle != null)
            {
                managerToggle.isOn = populationBalancer.enabledManager;
                managerToggle.onValueChanged.AddListener(v => populationBalancer.SetEnabledManager(v));
            }
        }

        if (geneticManager != null)
        {
            if (geneticsToggle != null)
            {
                geneticsToggle.isOn = geneticManager.geneticsEnabled;
                geneticsToggle.onValueChanged.AddListener(v => geneticManager.SetGeneticsEnabled(v));
            }

            if (mutationSlider != null)
            {
                mutationSlider.minValue = 0f;
                mutationSlider.maxValue = 1f;
                mutationSlider.value = geneticManager.mutationStrength;
                mutationSlider.onValueChanged.AddListener(v =>
                {
                    geneticManager.SetMutationStrength(v);
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

        if (mutationText != null && geneticManager != null)
            mutationText.text = $"Mutación: {geneticManager.mutationStrength:0.00}";
    }
}
