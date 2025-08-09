using UnityEngine;

public class VegetationSpawner : MonoBehaviour
{
    public GameObject vegetationPrefab;
    public int count = 100;
    public Vector2 areaSize = new Vector2(50, 50);

    [Header("Patch Settings")]
    public int patchCount = 5;
    public float patchRadius = 5f;

    void Start()
    {
        // Generar centros de parches
        Vector3[] centers = new Vector3[patchCount];
        for (int i = 0; i < patchCount; i++)
        {
            centers[i] = new Vector3(
                Random.Range(-areaSize.x / 2, areaSize.x / 2),
                0,
                Random.Range(-areaSize.y / 2, areaSize.y / 2)
            );
        }

        // Instanciar vegetación alrededor de los parches
        for (int i = 0; i < count; i++)
        {
            Vector3 center = centers[Random.Range(0, patchCount)];
            Vector2 offset = Random.insideUnitCircle * patchRadius;
            Vector3 pos = new Vector3(center.x + offset.x, 0, center.z + offset.y);
            Instantiate(vegetationPrefab, pos, Quaternion.identity);
        }
    }
}
