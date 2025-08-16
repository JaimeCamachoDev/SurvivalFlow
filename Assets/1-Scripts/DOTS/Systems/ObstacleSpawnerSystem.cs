using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Sistema que genera obstáculos aleatorios en la cuadrícula al inicio.
[BurstCompile]
public partial struct ObstacleSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonRW<ObstacleManager>(out var managerRw) ||
            !SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        if (managerRw.ValueRO.Initialized != 0)
            return;

        var manager = managerRw.ValueRO;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var rand = Unity.Mathematics.Random.CreateFromIndex(5);

        float2 area = grid.AreaSize;
        int2 half = (int2)(area / 2f);

        // Celdas ya ocupadas por plantas u otros obstáculos.
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
            if (!occupied.Add(cell))
                continue;

            var e = ecb.Instantiate(manager.Prefab);
            var pos = new float3(cell.x, 0f, cell.y);
            ecb.SetComponent(e, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f));
            ecb.SetComponent(e, new GridPosition { Cell = cell });
            spawned++;
        }

        occupied.Dispose();

        manager.Initialized = 1;
        managerRw.ValueRW = manager;
        ecb.Playback(state.EntityManager);
    }
}
