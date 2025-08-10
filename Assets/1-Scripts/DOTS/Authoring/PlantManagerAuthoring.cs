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
                MaxPlants = authoring.maxPlants
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
}
