using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Genera los agentes plantilla configurados por TemplateAgentManager.
[BurstCompile]
public partial struct TemplateAgentSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Comprobar que existan el manager y la información de la grilla.
        if (!SystemAPI.TryGetSingletonRW<TemplateAgentManager>(out var managerRw) ||
            !SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        // Evitar ejecutar más de una vez el spawner.
        if (managerRw.ValueRO.Initialized != 0)
            return;

        var manager = managerRw.ValueRO;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Generador aleatorio fijo para obtener posiciones iniciales.
        var rand = Unity.Mathematics.Random.CreateFromIndex(11);
        float2 area = grid.AreaSize;
        int2 half = (int2)(area / 2f);

        // Datos base del prefab del agente (sin modificar el prefab en sí).
        var prefabData = state.EntityManager.GetComponentData<TemplateAgent>(manager.Prefab);

        // Instanciar la cantidad solicitada de agentes.
        for (int i = 0; i < manager.Count; i++)
        {
            // Elegir una celda aleatoria dentro del área de juego.
            int2 cell = new int2(
                rand.NextInt(-half.x, half.x + 1),
                rand.NextInt(-half.y, half.y + 1));

            // Crear la entidad basada en el prefab.
            var e = ecb.Instantiate(manager.Prefab);

            // Posicionar la entidad en la celda seleccionada.
            ecb.SetComponent(e, new LocalTransform
            {
                Position = new float3(cell.x, 0f, cell.y),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            // Registrar la posición de la celda en el componente de grilla.
            ecb.AddComponent(e, new GridPosition { Cell = cell });

            // Copiar los datos del prefab y personalizarlos para esta instancia.
            var agent = prefabData;
            agent.Target = cell; // El primer objetivo es su posición actual.
            agent.MoveSpeed = manager.MoveSpeed; // Asignar velocidad desde el manager.
            ecb.SetComponent(e, agent);
        }

        // Marcar que la generación ya se realizó para no repetirla.
        manager.Initialized = 1;
        managerRw.ValueRW = manager;
        ecb.Playback(state.EntityManager);
    }
}
