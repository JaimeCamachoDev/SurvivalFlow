using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// Autoría que expone parámetros globales de las plantas.
public class PlantManagerAuthoring : MonoBehaviour
{
    public GameObject plantPrefab;
    [Range(0f, 1f)] public float density = 0.5f;
    public bool enforceDensity = true;
    public int maxPlants = 1000;

    public Vector2 areaSize = new Vector2(50, 50);
    public float reproductionInterval = 10f;
    public float minDistanceBetweenPlants = 1f;
    public float reproductionRadius = 3f;
    [Range(0f, 1f)] public float randomSpawnChance = 0.1f;

    class Baker : Baker<PlantManagerAuthoring>
    {
        public override void Bake(PlantManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PlantManager
            {
                Prefab = GetEntity(authoring.plantPrefab, TransformUsageFlags.Dynamic),
                Density = authoring.density,
                EnforceDensity = authoring.enforceDensity,
                MaxPlants = authoring.maxPlants,
                AreaSize = new float2(authoring.areaSize.x, authoring.areaSize.y),
                ReproductionInterval = authoring.reproductionInterval,
                MinDistance = authoring.minDistanceBetweenPlants,
                ReproductionRadius = authoring.reproductionRadius,
                RandomSpawnChance = authoring.randomSpawnChance,
                Timer = 0f
            });
        }
    }
}

public struct PlantManager : IComponentData
{
    public Entity Prefab;
    public float Density;
    public bool EnforceDensity;
    public int MaxPlants;
    public float2 AreaSize;
    public float ReproductionInterval;
    public float MinDistance;
    public float ReproductionRadius;
    public float RandomSpawnChance;
    public float Timer;
}
