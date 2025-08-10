using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// Autoría para configurar el generador de plantas.
public class PlantSpawnerAuthoring : MonoBehaviour
{
    public GameObject PlantPrefab;
    public int Count = 100;
    public Vector2 areaSize = new Vector2(50, 50);

    [Header("Patch Settings")]
    public int patchCount = 5;
    public float patchRadius = 5f;

    class Baker : Baker<PlantSpawnerAuthoring>
    {
        public override void Bake(PlantSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PlantSpawner
            {
                Prefab = GetEntity(authoring.PlantPrefab, TransformUsageFlags.Dynamic),
                Count = authoring.Count,
                AreaSize = new float2(authoring.areaSize.x, authoring.areaSize.y),
                PatchCount = authoring.patchCount,
                PatchRadius = authoring.patchRadius
            });
        }
    }
}

public struct PlantSpawner : IComponentData
{
    public Entity Prefab;
    public int Count;
    public float2 AreaSize;
    public int PatchCount;
    public float PatchRadius;
}
