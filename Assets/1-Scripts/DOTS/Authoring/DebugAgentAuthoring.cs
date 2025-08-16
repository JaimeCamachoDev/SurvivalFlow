using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// Autoría del prefab de agente de depuración.
public class DebugAgentAuthoring : MonoBehaviour
{
    // Velocidad de movimiento en celdas por segundo.
    public float moveSpeed = 5f;

    class Baker : Baker<DebugAgentAuthoring>
    {
        public override void Bake(DebugAgentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new DebugAgent
            {
                Target = int2.zero,
                MoveSpeed = authoring.moveSpeed
            });
            AddComponent(entity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            AddComponent(entity, new GridPosition { Cell = int2.zero });
        }
    }
}
