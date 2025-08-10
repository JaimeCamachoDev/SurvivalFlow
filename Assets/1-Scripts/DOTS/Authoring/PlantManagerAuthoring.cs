using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// Autoría que configura la gestión de plantas y el primer conjunto que se generará.
public class PlantManagerAuthoring : MonoBehaviour
{
    public GameObject plantPrefab;
    public int initialCount = 100;

    [Header("Patch Settings")]
    public int patchCount = 5;
    public float patchRadius = 5f;

    [Header("Reproduction")]
    [Range(0f,1f)]
    public float reproductionCost = 0.2f;

    class Baker : Baker<PlantManagerAuthoring>
    {
        public override void Bake(PlantManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PlantManager
            {
                Prefab = GetEntity(authoring.plantPrefab, TransformUsageFlags.Dynamic),
                InitialCount = authoring.initialCount,
                PatchCount = authoring.patchCount,
                PatchRadius = authoring.patchRadius,
                ReproductionCost = authoring.reproductionCost,
                Initialized = 0
            });
        }
    }
}

public struct PlantManager : IComponentData
{
    public Entity Prefab;
    public int InitialCount;
    public int PatchCount;
    public float PatchRadius;
    public float ReproductionCost;
    public byte Initialized;
}
