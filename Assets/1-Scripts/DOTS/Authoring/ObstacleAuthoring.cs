using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// Authoring para crear entidades de obstáculo en la cuadrícula.
public class ObstacleAuthoring : MonoBehaviour
{
    class Baker : Baker<ObstacleAuthoring>
    {
        public override void Bake(ObstacleAuthoring authoring)
        {
            // Obstáculos estáticos; se usa TransformUsageFlags.None (no existe "Static").
            var entity = GetEntity(TransformUsageFlags.None);

            float3 pos = authoring.transform.position;
            int2 cell = new int2((int)math.round(pos.x), (int)math.round(pos.z));

            AddComponent<ObstacleTag>(entity);
            AddComponent(entity, new GridPosition { Cell = cell });
            AddComponent(entity, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f));
        }
    }
}
