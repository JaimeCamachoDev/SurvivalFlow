using UnityEngine;

public class VegetationTile : MonoBehaviour
{
    public float maxGrowth = 100f;
    public float growthRate = 2f;
    [Range(0f, 1f)] public float initialGrowthPercent = 0.05f;
    public float reproductionCost = 30f;

    public float growth;

    public bool isAlive => growth > 0f;
    public bool IsMature => growth >= maxGrowth;

    void Start()
    {
        growth = maxGrowth * initialGrowthPercent;
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

    public void ReduceGrowthAfterReproduction()
    {
        growth -= reproductionCost;
        if (growth <= 0f)
            Destroy(gameObject);
    }
}
