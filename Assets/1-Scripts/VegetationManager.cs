using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Administra todas las plantas del mundo: registro de tiles, reproducción
/// cerca de plantas maduras y siembra aleatoria en áreas vacías.
/// </summary>
public class VegetationManager : MonoBehaviour
{
    public static VegetationManager Instance;

    public List<VegetationTile> activeVegetation = new List<VegetationTile>(); // Lista de plantas vivas

    [Header("Reproducción")]
    public GameObject vegetationPrefab;           // Prefab de la planta
    public Vector2 areaSize = new Vector2(50, 50); // Tamaño del mapa
    public float reproductionInterval = 10f;      // Cada cuánto intentan reproducirse
    public int maxVegetation = 200;               // Límite máximo de plantas
    public float minDistanceBetweenPlants = 1f;   // Distancia mínima entre plantas
    public float reproductionRadius = 3f;         // Radio alrededor de la planta madre
    [Range(0f,1f)] public float randomSpawnChance = 0.1f; // Probabilidad de semilla aleatoria

    float timer;

    void Awake()
    {
        // Patrón singleton simple para acceder al manager
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
        // Acumulamos tiempo hasta el siguiente intento de reproducción
        timer += Time.deltaTime;
        if (timer < reproductionInterval)
            return;

        timer = 0f;
        if (vegetationPrefab == null || activeVegetation.Count >= maxVegetation)
            return;

        // Intentamos generar alrededor de plantas maduras
        List<VegetationTile> maturePlants = new List<VegetationTile>();
        for (int i = 0; i < activeVegetation.Count; i++)
        {
            var v = activeVegetation[i];
            if (v != null && v.IsMature)
                maturePlants.Add(v);
        }
        if (maturePlants.Count > 0)
        {
            for (int i = 0; i < 10; i++)
            {
                var parent = maturePlants[Random.Range(0, maturePlants.Count)];
                Vector2 offset2 = Random.insideUnitCircle.normalized * Random.Range(minDistanceBetweenPlants, reproductionRadius);
                Vector3 pos = parent.transform.position + new Vector3(offset2.x, 0f, offset2.y);
                pos.x = Mathf.Clamp(pos.x, -areaSize.x / 2, areaSize.x / 2);
                pos.z = Mathf.Clamp(pos.z, -areaSize.y / 2, areaSize.y / 2);

                if (!InsideArea(pos))
                    continue;

                bool occupied = false;
                for (int j = 0; j < activeVegetation.Count; j++)
                {
                    if (Vector3.Distance(activeVegetation[j].transform.position, pos) < minDistanceBetweenPlants)
                    {
                        occupied = true;
                        break;
                    }
                }
                if (!occupied)
                {
                    Instantiate(vegetationPrefab, pos, Quaternion.identity);
                    parent.ReduceGrowthAfterReproduction();
                    break;
                }
            }
        }

        // Sembrado aleatorio para repoblar zonas vacías
        if (activeVegetation.Count < maxVegetation &&
            (activeVegetation.Count == 0 || Random.value < randomSpawnChance))
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-areaSize.x / 2, areaSize.x / 2),
                    0f,
                    Random.Range(-areaSize.y / 2, areaSize.y / 2));

                bool occupied = false;
                for (int j = 0; j < activeVegetation.Count; j++)
                {
                    if (Vector3.Distance(activeVegetation[j].transform.position, pos) < minDistanceBetweenPlants)
                    {
                        occupied = true;
                        break;
                    }
                }
                if (!occupied)
                {
                    Instantiate(vegetationPrefab, pos, Quaternion.identity);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Siembra nuevas plantas alrededor de un punto dado, usado por la carne al descomponerse.
    /// </summary>
    public void FertilizeArea(Vector3 center, float radius, int attempts = 1)
    {
        if (vegetationPrefab == null || activeVegetation.Count >= maxVegetation)
            return;

        for (int i = 0; i < attempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 pos = center + new Vector3(offset.x, 0f, offset.y);
            pos.x = Mathf.Clamp(pos.x, -areaSize.x / 2, areaSize.x / 2);
            pos.z = Mathf.Clamp(pos.z, -areaSize.y / 2, areaSize.y / 2);
            if (!InsideArea(pos))
                continue;

            bool occupied = false;
            for (int j = 0; j < activeVegetation.Count; j++)
            {
                if (Vector3.Distance(activeVegetation[j].transform.position, pos) < minDistanceBetweenPlants)
                {
                    occupied = true;
                    break;
                }
            }
            if (!occupied)
                Instantiate(vegetationPrefab, pos, Quaternion.identity);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaSize.x, 0.1f, areaSize.y));
    }

    // Comprueba si una posición cae dentro del área válida de juego
    bool InsideArea(Vector3 pos)
    {
        return Mathf.Abs(pos.x) <= areaSize.x / 2 && Mathf.Abs(pos.z) <= areaSize.y / 2;
    }
}
