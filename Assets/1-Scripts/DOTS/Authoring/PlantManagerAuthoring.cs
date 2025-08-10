using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// Autoría que expone parámetros globales de las plantas.
public class PlantManagerAuthoring : MonoBehaviour
{
    public GameObject plantPrefab;
    [Range(0, 1f)] public float reproductionCost = 0.3f;
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
                OvercrowdLimit = authoring.overcrowdLimit
            });
        }
    }
}

public struct PlantManager : IComponentData
{
    public Entity Prefab;
    public float ReproductionCost;
    public int OvercrowdLimit;
}
