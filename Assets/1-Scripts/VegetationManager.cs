using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VegetationManager : MonoBehaviour
{
    public static VegetationManager Instance;

    public List<VegetationTile> activeVegetation = new List<VegetationTile>();

    [Header("Reproducción")]
    public GameObject vegetationPrefab;
    public Vector2 areaSize = new Vector2(50, 50);
    public float reproductionInterval = 10f;
    public int maxVegetation = 200;
    public float minDistanceBetweenPlants = 1f;
    public float reproductionRadius = 3f;

    float timer;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public void Register(VegetationTile tile)
    {
        if (!activeVegetation.Contains(tile))
            activeVegetation.Add(tile);
    }

    public void Unregister(VegetationTile tile)
    {
        if (activeVegetation.Contains(tile))
            activeVegetation.Remove(tile);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < reproductionInterval)
            return;

        timer = 0f;
        if (vegetationPrefab == null || activeVegetation.Count >= maxVegetation)
            return;

        var maturePlants = activeVegetation.Where(v => v.IsMature).ToList();
        if (maturePlants.Count == 0)
            return;

        for (int i = 0; i < 10; i++)
        {
            var parent = maturePlants[Random.Range(0, maturePlants.Count)];
            Vector2 offset2 = Random.insideUnitCircle.normalized * Random.Range(minDistanceBetweenPlants, reproductionRadius);
            Vector3 pos = parent.transform.position + new Vector3(offset2.x, 0f, offset2.y);

            if (!InsideArea(pos))
                continue;

            bool occupied = activeVegetation.Any(v => Vector3.Distance(v.transform.position, pos) < minDistanceBetweenPlants);
            if (!occupied)
            {
                Instantiate(vegetationPrefab, pos, Quaternion.identity);
                parent.ReduceGrowthAfterReproduction();
                break;
            }
        }
    }

    bool InsideArea(Vector3 pos)
    {
        return Mathf.Abs(pos.x) <= areaSize.x / 2 && Mathf.Abs(pos.z) <= areaSize.y / 2;
    }
}
