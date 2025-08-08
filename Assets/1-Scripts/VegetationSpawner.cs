using UnityEngine;

public class VegetationSpawner : MonoBehaviour
{
    public GameObject vegetationPrefab;
    public int count = 100;
    public Vector2 areaSize = new Vector2(50, 50);

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-areaSize.x / 2, areaSize.x / 2),
                0,
                Random.Range(-areaSize.y / 2, areaSize.y / 2)
            );
            Instantiate(vegetationPrefab, pos, Quaternion.identity);
        }
    }
}

