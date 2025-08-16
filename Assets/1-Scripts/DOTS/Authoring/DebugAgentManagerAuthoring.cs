using Unity.Entities;
using UnityEngine;

/// Autoría para configurar la generación de agentes de depuración.
public class DebugAgentManagerAuthoring : MonoBehaviour
{
    // Prefab del agente de depuración.
    public GameObject agentPrefab;

    // Número de agentes iniciales.
    public int count = 5;

    class Baker : Baker<DebugAgentManagerAuthoring>
    {
        public override void Bake(DebugAgentManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new DebugAgentManager
            {
                Prefab = GetEntity(authoring.agentPrefab, TransformUsageFlags.Dynamic),
                Count = authoring.count,
                Initialized = 0
            });
        }
    }
}

/// Componente singleton que controla a los agentes de depuración.
public struct DebugAgentManager : IComponentData
{
    public Entity Prefab;
    public int Count;
    public byte Initialized;
}
