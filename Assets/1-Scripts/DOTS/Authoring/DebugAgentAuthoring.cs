using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// Autoría del prefab de agente de depuración.
public class DebugAgentAuthoring : MonoBehaviour
{
    // Velocidad de movimiento base en celdas por segundo.
    // El manager puede sobreescribir este valor para todos los agentes.
    public float moveSpeed = 5f;

    class Baker : Baker<DebugAgentAuthoring>
    {
        // Convierte el prefab de GameObject a una entidad con los componentes necesarios.
        public override void Bake(DebugAgentAuthoring authoring)
        {
            // Crear una entidad dinámica para instanciar en tiempo de ejecución.
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Registrar el componente DebugAgent con la velocidad base y sin objetivo.
            AddComponent(entity, new DebugAgent
            {
                Target = int2.zero,
                MoveSpeed = authoring.moveSpeed
            });

            // Añadir un transform local para posicionar la entidad en el mundo.
            AddComponent(entity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));

            // Cada agente necesita conocer la celda que ocupa en la grilla.
            AddComponent(entity, new GridPosition { Cell = int2.zero });
        }
    }
}
