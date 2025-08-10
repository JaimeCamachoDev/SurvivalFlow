using UnityEngine;

/// <summary>
/// Representa una planta individual que crece con el tiempo, puede ser
/// consumida por herbívoros y reproducirse perdiendo parte de su energía.
/// </summary>
public class VegetationTile : MonoBehaviour
{
    public float maxGrowth = 100f;                     // Tamaño máximo
    public float growthRate = 2f;                      // Velocidad de crecimiento
    [Range(0f, 1f)] public float initialGrowthPercent = 0.05f; // Tamaño inicial como porcentaje
    public float reproductionCost = 30f;               // Energía que pierde al reproducirse

    public float growth;
    Vector3 baseScale;
    Renderer cachedRenderer;                          // Cache del Renderer
    Color baseColor;                                   // Color original del material
    bool shrinking;                                    // Indica si fue consumida recientemente

    public bool isAlive => growth > 0f;                // Sigue en el mundo
    public bool IsMature => growth >= maxGrowth;       // Puede reproducirse

    void Start()
    {
        baseScale = transform.localScale;                         // Escala base del prefab
        cachedRenderer = GetComponent<Renderer>();                // Obtenemos y guardamos el Renderer
        if (cachedRenderer != null)
            baseColor = cachedRenderer.material.color;
        growth = maxGrowth * initialGrowthPercent;                // Se inicia como plántula
        UpdateScale();
        UpdateColor();
        VegetationManager.Instance?.Register(this);               // Se registra en el manager
    }

    void OnDestroy()
    {
        if (VegetationManager.Instance != null)
            VegetationManager.Instance.Unregister(this);          // Se elimina de la lista global
    }

    void Update()
    {
        if (growth <= 0f)
            return;

        // Crecimiento continuo hasta la madurez
        if (growth < maxGrowth)
        {
            growth += growthRate * Time.deltaTime;
            growth = Mathf.Min(growth, maxGrowth);
            UpdateScale();
        }
        UpdateColor();
        shrinking = false; // Solo se mantiene roja un frame tras ser consumida
    }

    // Reduce el crecimiento al ser comida y devuelve cuánto se consumió
    public float Consume(float amount)
    {
        float consumed = Mathf.Min(amount, growth);
        growth -= consumed;
        UpdateScale();
        shrinking = true;
        UpdateColor();

        if (growth <= 0f)
            Destroy(gameObject);

        return consumed;
    }
    // Llamado por el manager cuando esta planta genera una hija
    public void ReduceGrowthAfterReproduction()
    {
        growth -= reproductionCost;
        UpdateScale();
        shrinking = true;
        UpdateColor();
        if (growth <= 0f)
            Destroy(gameObject);
    }
    // Ajusta la escala visual de la planta según su crecimiento
    void UpdateScale()
    {
        float t = Mathf.Max(growth / maxGrowth, 0f);
        transform.localScale = baseScale * t;
    }

    // Cambia el color según el estado de crecimiento
    void UpdateColor()
    {
        if (!VisualCueSettings.enableVisualCues || cachedRenderer == null || VisualCueSettings.Instance == null)
            return;

        if (shrinking)
            cachedRenderer.material.color = VisualCueSettings.Instance.plantConsumedColor; // Consumida
        else if (growth >= maxGrowth)
            cachedRenderer.material.color = VisualCueSettings.Instance.plantMatureColor;   // Madura
        else if (growth < maxGrowth * 0.5f)
            cachedRenderer.material.color = VisualCueSettings.Instance.plantGrowingColor;  // Creciendo
        else
            cachedRenderer.material.color = baseColor;                                     // Estado intermedio
    }
}
