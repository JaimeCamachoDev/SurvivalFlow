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

    public bool isAlive => growth > 0f;                // Sigue en el mundo
    public bool IsMature => growth >= maxGrowth;       // Puede reproducirse

    void Start()
    {
        baseScale = transform.localScale;                         // Escala base del prefab
        growth = maxGrowth * initialGrowthPercent;                // Se inicia como plántula
        UpdateScale();
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
    }

    // Reduce el crecimiento al ser comida y devuelve cuánto se consumió
    public float Consume(float amount)
    {
        float consumed = Mathf.Min(amount, growth);
        growth -= consumed;
        UpdateScale();

        if (growth <= 0f)
            Destroy(gameObject);

        return consumed;
    }
    // Llamado por el manager cuando esta planta genera una hija
    public void ReduceGrowthAfterReproduction()
    {
        growth -= reproductionCost;
        UpdateScale();
        if (growth <= 0f)
            Destroy(gameObject);
    }
    // Ajusta la escala visual de la planta según su crecimiento
    void UpdateScale()
    {
        float t = Mathf.Max(growth / maxGrowth, 0f);
        transform.localScale = baseScale * t;
    }
}
