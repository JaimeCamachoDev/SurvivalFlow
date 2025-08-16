using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// Autoría del prefab de la plantilla de movimiento.
public class MovementTemplateAgentAuthoring : MonoBehaviour
{
    // Velocidad de movimiento base en celdas por segundo.
    // El manager puede sobreescribir este valor para todas las instancias.
    public float moveSpeed = 5f;

    class Baker : Baker<MovementTemplateAgentAuthoring>
    {
        // Convierte el prefab de GameObject a una entidad con los componentes necesarios.
        public override void Bake(MovementTemplateAgentAuthoring authoring)
        {
            // Crear una entidad dinámica para instanciar en tiempo de ejecución.
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Registrar el componente de movimiento base con la velocidad inicial y sin objetivo.
            AddComponent(entity, new MovementTemplateAgent
            {
                Target = int2.zero,
                MoveSpeed = authoring.moveSpeed
            });

            // Añadir un buffer para almacenar el camino calculado entre celdas.
            AddBuffer<MovementPathElement>(entity);

            // Añadir un transform local para posicionar la entidad en el mundo.
            AddComponent(entity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));

            // Cada agente necesita conocer la celda que ocupa en la grilla.
            AddComponent(entity, new GridPosition { Cell = int2.zero });
        }
    }
}
