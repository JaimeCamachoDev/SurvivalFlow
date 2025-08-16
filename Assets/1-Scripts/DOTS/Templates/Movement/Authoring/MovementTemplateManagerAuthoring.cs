using Unity.Entities;
using UnityEngine;

/// Autoría para configurar la generación de entidades basadas en la plantilla de movimiento.
public class MovementTemplateManagerAuthoring : MonoBehaviour
{
    // Prefab que implementa la plantilla de movimiento.
    public GameObject agentPrefab;

    // Número de instancias iniciales.
    public int count = 5;

    // Velocidad de movimiento que se aplicará a todas las instancias.
    public float moveSpeed = 4f;

    class Baker : Baker<MovementTemplateManagerAuthoring>
    {
        // Convierte los datos de la escena en componentes ECS.
        public override void Bake(MovementTemplateManagerAuthoring authoring)
        {
            // Obtener la entidad que representará al manager.
            var entity = GetEntity(TransformUsageFlags.None);

            // Registrar un componente singleton con todos los parámetros necesarios
            // para instanciar y configurar entidades que usen la plantilla de movimiento.
            AddComponent(entity, new MovementTemplateManager
            {
                // Referencia al prefab convertido a entidad.
                Prefab = GetEntity(authoring.agentPrefab, TransformUsageFlags.Dynamic),
                // Cantidad de entidades a generar.
                Count = authoring.count,
                // Velocidad de movimiento común.
                MoveSpeed = authoring.moveSpeed,
                // Bandera utilizada para saber si ya se generaron las instancias.
                Initialized = 0
            });
        }
    }
}

/// Componente singleton que controla a las entidades que usan la plantilla de movimiento.
public struct MovementTemplateManager : IComponentData
{
    public Entity Prefab;
    public int Count;
    public float MoveSpeed;
    public byte Initialized;
}
