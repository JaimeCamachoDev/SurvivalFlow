using Unity.Entities;
using UnityEngine;

/// Authoring que define la configuración para generar obstáculos aleatorios en la cuadrícula.
public class ObstacleManagerAuthoring : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public int count = 25;

    class Baker : Baker<ObstacleManagerAuthoring>
    {
        public override void Bake(ObstacleManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ObstacleManager
            {
                Prefab = GetEntity(authoring.obstaclePrefab, TransformUsageFlags.Dynamic),
                Count = authoring.count,
                Initialized = 0,
                DebugCrossings = 0
            });
        }
    }
}

/// Componente que almacena la configuración global de obstáculos.
public struct ObstacleManager : IComponentData
{
    public Entity Prefab;
    public int Count;
    public byte Initialized;
    public int DebugCrossings;
}
