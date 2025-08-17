using Unity.Entities;
using UnityEngine;

/// Autoría para configurar la generación de agentes plantilla.
public class TemplateAgentManagerAuthoring : MonoBehaviour
{
    // Prefab del agente plantilla.
    public GameObject agentPrefab;

    // Número de agentes iniciales.
    public int count = 5;
    // Velocidad de movimiento que se aplicará a todos los agentes.
    public float moveSpeed = 4f;

    class Baker : Baker<TemplateAgentManagerAuthoring>
    {
        // Convierte los datos de la escena en componentes ECS.
        public override void Bake(TemplateAgentManagerAuthoring authoring)
        {
            // Obtener la entidad que representará al manager.
            var entity = GetEntity(TransformUsageFlags.None);

            // Registrar un componente singleton con todos los parámetros necesarios
            // para instanciar y configurar los agentes plantilla.
            AddComponent(entity, new TemplateAgentManager
            {
                // Referencia al prefab convertido a entidad.
                Prefab = GetEntity(authoring.agentPrefab, TransformUsageFlags.Dynamic),
                // Cantidad de agentes a generar.
                Count = authoring.count,
                // Velocidad de movimiento común.
                MoveSpeed = authoring.moveSpeed,
                // Bandera utilizada para saber si ya se generaron los agentes.
                Initialized = 0
            });
        }
    }
}

/// Componente singleton que controla a los agentes plantilla.
public struct TemplateAgentManager : IComponentData
{
    public Entity Prefab;
    public int Count;
    public float MoveSpeed;
    public byte Initialized;
}
