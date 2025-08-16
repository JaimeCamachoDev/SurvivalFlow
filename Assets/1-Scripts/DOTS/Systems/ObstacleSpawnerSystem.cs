using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Sistema que genera obstáculos aleatorios en la cuadrícula al inicio.
/// Los obstáculos se colocan en celdas libres y se marcan con `ObstacleTag`
/// para que otros sistemas (como los agentes de depuración) los eviten.
[BurstCompile]
public partial struct ObstacleSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Obtener el manager de obstáculos y la información de la grilla.
        if (!SystemAPI.TryGetSingletonRW<ObstacleManager>(out var managerRw) ||
            !SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        // Ejecutar solo una vez al inicio.
        if (managerRw.ValueRO.Initialized != 0)
            return;

        var manager = managerRw.ValueRO;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        bool prefabHasGridPos = state.EntityManager.HasComponent<GridPosition>(manager.Prefab);
        bool prefabHasObstacleTag = state.EntityManager.HasComponent<ObstacleTag>(manager.Prefab);
        // Generador aleatorio con semilla fija para reproducibilidad.
        var rand = Unity.Mathematics.Random.CreateFromIndex(5);

        float2 area = grid.AreaSize;
        int2 half = (int2)(area / 2f);

        // Celdas ya ocupadas por obstáculos existentes o plantas.
        var occupied = new NativeParallelHashSet<int2>(manager.Count, Allocator.Temp);
        foreach (var gp in SystemAPI.Query<RefRO<GridPosition>>().WithAll<ObstacleTag>())
            occupied.Add(gp.ValueRO.Cell);
        foreach (var gp in SystemAPI.Query<RefRO<GridPosition>>().WithAll<Plant>())
            occupied.Add(gp.ValueRO.Cell);

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = manager.Count * 20;
        while (spawned < manager.Count && attempts < maxAttempts)
        {
            attempts++;
            int2 cell = new int2(
                rand.NextInt(-half.x, half.x + 1),
                rand.NextInt(-half.y, half.y + 1));
            // Si la celda ya está ocupada se intenta con otra.
            if (!occupied.Add(cell))
                continue;

            var e = ecb.Instantiate(manager.Prefab);
            var pos = new float3(cell.x, 0f, cell.y);
            // Posicionar el obstáculo en el mundo.
            ecb.SetComponent(e, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f));
            // Asegurar que tenga GridPosition.
            if (prefabHasGridPos)
                ecb.SetComponent(e, new GridPosition { Cell = cell });
            else
                ecb.AddComponent(e, new GridPosition { Cell = cell });
            // Asegurar que posea la etiqueta de obstáculo.
            if (!prefabHasObstacleTag)
                ecb.AddComponent<ObstacleTag>(e);
            spawned++;
        }

        occupied.Dispose();

        // Guardar que ya se generaron los obstáculos.
        manager.Initialized = 1;
        managerRw.ValueRW = manager;
        ecb.Playback(state.EntityManager);
    }
}
