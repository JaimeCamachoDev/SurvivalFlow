using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Monitors the populations of plants, herbivores and carnivores and
/// attempts to keep herbivores and carnivores above a minimum amount.
/// If a population drops too low, reproduction thresholds are temporarily
/// lowered and additional prefabs can be spawned near existing members.
/// The manager can be disabled so the simulation runs without intervention.
/// </summary>
public class PopulationBalancer : MonoBehaviour
{
    [Header("Manager")]
    public bool enabledManager = true;
    public float checkInterval = 5f;

    [Header("Population minimums")]
    public int minHerbivores = 5;
    public int minCarnivores = 3;

    [Header("Population maximums")]
    public int maxHerbivores = 30;
    public int maxCarnivores = 10;

    [Header("Reproduction thresholds")]
    public float herbivoreNormalThreshold = 80f;
    public float herbivoreBoostedThreshold = 40f;
    public float carnivoreNormalThreshold = 80f;
    public float carnivoreBoostedThreshold = 40f;

    [Header("Spawning")]
    public GameObject herbivorePrefab;
    public GameObject carnivorePrefab;
    public float spawnRadius = 2f;
    public int spawnAmount = 1;

    [Header("Current counts")]
    public int currentPlants;
    public int currentHerbivores;
    public int currentCarnivores;

    float timer;

    void Update()
    {
        if (!enabledManager)
            return;

        timer += Time.deltaTime;
        if (timer < checkInterval)
            return;
        timer = 0f;

        // Sample populations (similar to PopulationGraph.Update)
        currentPlants = VegetationManager.Instance != null ?
            VegetationManager.Instance.activeVegetation.Count : 0;
        List<Herbivores> herbArray = Herbivores.All;
        List<Carnivore> carnArray = Carnivore.All;
        currentHerbivores = herbArray.Count;
        currentCarnivores = carnArray.Count;

        // Herbivore balancing
        if (currentHerbivores < minHerbivores)
        {
            AdjustHerbivoreReproduction(herbArray, herbivoreBoostedThreshold);
            SpawnNearExisting(herbivorePrefab, herbArray, currentHerbivores, maxHerbivores);
        }
        else if (currentHerbivores >= maxHerbivores)
        {
            AdjustHerbivoreReproduction(herbArray, float.MaxValue);
        }
        else
        {
            AdjustHerbivoreReproduction(herbArray, herbivoreNormalThreshold);
        }

        // Carnivore balancing
        if (currentCarnivores < minCarnivores)
        {
            AdjustCarnivoreReproduction(carnArray, carnivoreBoostedThreshold);
            SpawnNearExisting(carnivorePrefab, carnArray, currentCarnivores, maxCarnivores);
        }
        else if (currentCarnivores >= maxCarnivores)
        {
            AdjustCarnivoreReproduction(carnArray, float.MaxValue);
        }
        else
        {
            AdjustCarnivoreReproduction(carnArray, carnivoreNormalThreshold);
        }
    }

    void AdjustHerbivoreReproduction(List<Herbivores> herd, float threshold)
    {
        foreach (var h in herd)
            h.reproductionThreshold = threshold;
    }

    void AdjustCarnivoreReproduction(List<Carnivore> pack, float threshold)
    {
        foreach (var c in pack)
            c.reproductionThreshold = threshold;
    }

    void SpawnNearExisting<T>(GameObject prefab, List<T> existing, int currentCount, int maxCount) where T : MonoBehaviour
    {
        if (prefab == null || existing == null || existing.Count == 0 || currentCount >= maxCount)
            return;

        for (int i = 0; i < spawnAmount && currentCount < maxCount; i++, currentCount++)
        {
            Vector3 origin = existing[Random.Range(0, existing.Count)].transform.position;
            Vector3 pos = origin + new Vector3(Random.Range(-spawnRadius, spawnRadius), 0f,
                                              Random.Range(-spawnRadius, spawnRadius));
            Instantiate(prefab, pos, Quaternion.identity);
        }
    }

    public void SetEnabledManager(bool enabled)
    {
        enabledManager = enabled;
        if (!enabled)
            ResetThresholds();
    }

    void ResetThresholds()
    {
        foreach (var h in Herbivores.All)
            h.reproductionThreshold = herbivoreNormalThreshold;
        foreach (var c in Carnivore.All)
            c.reproductionThreshold = carnivoreNormalThreshold;
    }
}

