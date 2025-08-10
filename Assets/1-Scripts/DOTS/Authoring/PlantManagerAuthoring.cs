using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// Autoría que expone parámetros globales de las plantas.
public class PlantManagerAuthoring : MonoBehaviour
{
    public GameObject plantPrefab;
    [Range(0, 1f)] public float reproductionCost = 0.3f;
    public int underpopulationLimit = 2;
    public int reproductionThreshold = 3;
    public int overcrowdLimit = 5;

    class Baker : Baker<PlantManagerAuthoring>
    {
        public override void Bake(PlantManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PlantManager
            {
                Prefab = GetEntity(authoring.plantPrefab, TransformUsageFlags.Dynamic),
                ReproductionCost = authoring.reproductionCost,
                UnderpopulationLimit = authoring.underpopulationLimit,
                OvercrowdLimit = authoring.overcrowdLimit,
                ReproductionThreshold = authoring.reproductionThreshold
            });
        }
    }
}

public struct PlantManager : IComponentData
{
    public Entity Prefab;
    public float ReproductionCost;
    public int UnderpopulationLimit;
    public int OvercrowdLimit;
    public int ReproductionThreshold;
}
