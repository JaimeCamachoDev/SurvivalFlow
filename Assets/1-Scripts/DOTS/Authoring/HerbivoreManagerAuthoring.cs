using Unity.Entities;
using UnityEngine;

/// Authoring that configures spawning of DOTS herbivores.
public class HerbivoreManagerAuthoring : MonoBehaviour
{
    public GameObject herbivorePrefab;
    public int initialCount = 20;

    class Baker : Baker<HerbivoreManagerAuthoring>
    {
        public override void Bake(HerbivoreManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new HerbivoreManager
            {
                Prefab = GetEntity(authoring.herbivorePrefab, TransformUsageFlags.Dynamic),
                InitialCount = authoring.initialCount,
                Initialized = 0
            });
        }
    }
}

public struct HerbivoreManager : IComponentData
{
    public Entity Prefab;
    public int InitialCount;
    public byte Initialized;
}
