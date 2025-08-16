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
        bool prefabHasGridPos = state.EntityManager.HasComponent<GridPosition>(manager.Prefab);
        bool prefabHasObstacleTag = state.EntityManager.HasComponent<ObstacleTag>(manager.Prefab);
        var rand = Unity.Mathematics.Random.CreateFromIndex(5);

        float2 area = grid.AreaSize;
        int2 half = (int2)(area / 2f);

        // Celdas ya ocupadas por plantas u otros obstáculos.
        var obstacleQuery = SystemAPI.QueryBuilder().WithAll<GridPosition, ObstacleTag>().Build();
        var plantQuery = SystemAPI.QueryBuilder().WithAll<GridPosition, Plant>().Build();
        int obstacleCount = obstacleQuery.CalculateEntityCount();
        int plantCount = plantQuery.CalculateEntityCount();
        var occupied = new NativeParallelHashSet<int2>(manager.Count + obstacleCount + plantCount, Allocator.Temp);

        var obstacleCells = obstacleQuery.ToComponentDataArray<GridPosition>(Allocator.Temp);
        for (int i = 0; i < obstacleCells.Length; i++)
            occupied.Add(obstacleCells[i].Cell);
        obstacleCells.Dispose();

        var plantCells = plantQuery.ToComponentDataArray<GridPosition>(Allocator.Temp);
        for (int i = 0; i < plantCells.Length; i++)
            occupied.Add(plantCells[i].Cell);
        plantCells.Dispose();

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
            if (prefabHasGridPos)
                ecb.SetComponent(e, new GridPosition { Cell = cell });
            else
                ecb.AddComponent(e, new GridPosition { Cell = cell });
            if (!prefabHasObstacleTag)
                ecb.AddComponent<ObstacleTag>(e);
            spawned++;
        }

        occupied.Dispose();

        manager.Initialized = 1;
        managerRw.ValueRW = manager;
        ecb.Playback(state.EntityManager);
    }
}
