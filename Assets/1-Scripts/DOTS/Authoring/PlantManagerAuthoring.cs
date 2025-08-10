using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// Autoría que expone parámetros globales de las plantas.
public class PlantManagerAuthoring : MonoBehaviour
{
    public GameObject plantPrefab;
    public int maxPlants = 1000;
    public int reproductionThreshold = 4;

    public Vector2 areaSize = new Vector2(50, 50);
    public float reproductionInterval = 10f;
    public float minDistanceBetweenPlants = 1f;
    public float reproductionRadius = 3f;
    [Range(0f, 1f)] public float randomSpawnChance = 0.1f;

    public Vector2 areaSize = new Vector2(50, 50);
    public float reproductionInterval = 10f;
    public int offspringCount = 1;
    public int reproductionRadius = 1;
    public float minDistanceBetweenPlants = 1f;
    public float reproductionCost = 0.1f;
    [Range(0f, 1f)] public float randomSpawnChance = 0.1f;

    public Vector2 areaSize = new Vector2(50, 50);
    public float minDistanceBetweenPlants = 1f;
    public float reproductionCost = 0.1f;
    [Range(0f, 1f)] public float randomSpawnChance = 0.1f;

    class Baker : Baker<PlantManagerAuthoring>
    {
        public override void Bake(PlantManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PlantManager
            {
                Prefab = GetEntity(authoring.plantPrefab, TransformUsageFlags.Dynamic),
                MaxPlants = authoring.maxPlants,
                AreaSize = new float2(authoring.areaSize.x, authoring.areaSize.y),
                MinDistance = authoring.minDistanceBetweenPlants,
                ReproductionCost = authoring.reproductionCost,
                RandomSpawnChance = authoring.randomSpawnChance
            });
        }
    }
}

public struct PlantManager : IComponentData
{
    public Entity Prefab;
    public int MaxPlants;
    public float2 AreaSize;
    public float MinDistance;
    public float ReproductionCost;
    public float RandomSpawnChance;
}
