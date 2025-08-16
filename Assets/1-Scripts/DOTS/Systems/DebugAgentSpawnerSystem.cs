using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Genera los agentes de depuración configurados por DebugAgentManager.
[BurstCompile]
public partial struct DebugAgentSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonRW<DebugAgentManager>(out var managerRw) ||
            !SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        if (managerRw.ValueRO.Initialized != 0)
            return;

        var manager = managerRw.ValueRO;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var rand = Unity.Mathematics.Random.CreateFromIndex(11);
        float2 area = grid.AreaSize;
        int2 half = (int2)(area / 2f);

        var prefabData = state.EntityManager.GetComponentData<DebugAgent>(manager.Prefab);

        for (int i = 0; i < manager.Count; i++)
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
            var agent = prefabData;
            agent.Target = cell;
            ecb.SetComponent(e, agent);
        }

        manager.Initialized = 1;
        managerRw.ValueRW = manager;
        ecb.Playback(state.EntityManager);
    }
}
