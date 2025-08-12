using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// Authoring para crear entidades de obstáculo en la cuadrícula.
public class ObstacleAuthoring : MonoBehaviour
{
    class Baker : Baker<ObstacleAuthoring>
    {
        public override void Bake(ObstacleAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Static);
            AddComponent<ObstacleTag>(entity);
            AddComponent(entity, new GridPosition { Cell = int2.zero });
        }
    }
}
