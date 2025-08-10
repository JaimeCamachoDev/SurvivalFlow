using Unity.Entities;
using UnityEngine;

/// Componente clásico que se convierte a Entity en el build.
public class SpawnerAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public int Count = 1000;

    class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Spawner
            {
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                Count = authoring.Count
            });
        }
    }
}

public struct Spawner : IComponentData
{
    public Entity Prefab;
    public int Count;
}
