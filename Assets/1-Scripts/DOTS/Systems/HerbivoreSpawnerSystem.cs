using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Spawns the initial herbivores as configured by HerbivoreManager.
[BurstCompile]
public partial struct HerbivoreSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonRW<HerbivoreManager>(out var managerRw) ||
            !SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        if (managerRw.ValueRO.Initialized != 0)
            return;

        var manager = managerRw.ValueRO;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var rand = Unity.Mathematics.Random.CreateFromIndex(3);

        float2 area = grid.AreaSize;
        int2 half = (int2)(area / 2f);
        for (int i = 0; i < manager.InitialCount; i++)
        {
            int2 cell = new int2(
                rand.NextInt(-half.x, half.x + 1),
                rand.NextInt(-half.y, half.y + 1));

            var e = ecb.Instantiate(manager.Prefab);
            ecb.SetComponent(e, new LocalTransform
            {
                Position = new float3(cell.x, 0f, cell.y),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            ecb.AddComponent(e, new GridPosition { Cell = cell });
        }

        manager.Initialized = 1;
        managerRw.ValueRW = manager;
        ecb.Playback(state.EntityManager);
    }
}
