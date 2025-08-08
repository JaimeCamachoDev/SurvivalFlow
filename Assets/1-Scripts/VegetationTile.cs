using UnityEngine;

public class VegetationTile : MonoBehaviour
{
    public float growth = 100f;
    public float maxGrowth = 100f;
    public float growthRate = 2f;

    public bool isAlive => growth > 0f;

    void Start()
    {
        // Registrar la vegetación una vez que el manager esté listo
        VegetationManager.Instance?.Register(this);
    }

    void OnDestroy()
    {
        if (VegetationManager.Instance != null)
            VegetationManager.Instance.Unregister(this);
    }

    void Update()
    {
        if (growth <= 0f)
            return;

        if (growth < maxGrowth)
        {
            growth += growthRate * Time.deltaTime;
            growth = Mathf.Min(growth, maxGrowth);
        }
    }

    public float Consume(float amount)
    {
        float consumed = Mathf.Min(amount, growth);
        growth -= consumed;

        if (growth <= 0f)
            Destroy(gameObject);

        return consumed;
    }
}
