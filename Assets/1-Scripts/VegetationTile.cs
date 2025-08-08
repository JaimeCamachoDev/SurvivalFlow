using UnityEngine;

public class VegetationTile : MonoBehaviour
{
    public float growth = 100f;
    public float maxGrowth = 100f;
    public float growthRate = 2f;

    public bool isAlive => growth > 0f;

    void Awake()
    {
        VegetationManager.Instance?.Register(this);
    }

    void OnDestroy()
    {
        if (VegetationManager.Instance != null)
            VegetationManager.Instance.Unregister(this);
    }

    void Update()
    {
        if (growth < maxGrowth)
        {
            growth += growthRate * Time.deltaTime;
            growth = Mathf.Min(growth, maxGrowth);
        }

        if (growth <= 1)
        {
              Destroy(this.gameObject);
            Debug.Log("holii");
        }
    }

    public float Consume(float amount)
    {
        float consumed = Mathf.Min(amount, growth);
        growth -= consumed;
        return consumed;
    }
}
