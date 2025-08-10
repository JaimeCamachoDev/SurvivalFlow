using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Gestiona la reproducción y muerte por sobrepoblación en la cuadricula.
[BurstCompile]
public partial struct PlantGridSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<PlantManager>(out var manager))
            return;

        var query = SystemAPI.QueryBuilder().WithAll<Plant, GridPosition>().Build();
        int count = query.CalculateEntityCount();
        var occupancy = new NativeParallelHashMap<int2, byte>(count, Allocator.Temp);
        var births = new NativeParallelHashMap<int2, int>(count * 8, Allocator.Temp);
        var prefabPlant = state.EntityManager.GetComponentData<Plant>(manager.Prefab);

        foreach (var gp in SystemAPI.Query<RefRO<GridPosition>>())
        {
            occupancy.TryAdd(gp.ValueRO.Cell, 0);
        }

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (plant, gp) in SystemAPI.Query<RefRW<Plant>, RefRO<GridPosition>>())
        {
            int neighbours = 0;
            int children = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int2 check = gp.ValueRO.Cell + new int2(dx, dz);
                    if (occupancy.ContainsKey(check))
                    {
                        neighbours++;
                    }
                    else if (plant.ValueRO.Stage == PlantStage.Mature)
                    {
                        ref var countRef = ref births.GetValueRef(check, out var exists);
                        if (!exists) countRef = 0;
                        countRef++;
                        children++;
                    }
                }
            }

            if (neighbours < manager.UnderpopulationLimit || neighbours > manager.OvercrowdLimit)
            {
                plant.ValueRW.Stage = PlantStage.Withering;
            }
            else if (plant.ValueRO.Stage == PlantStage.Withering)
            {
                plant.ValueRW.Stage = PlantStage.Growing;
            }

            if (children > 0)
            {
                plant.ValueRW.Growth -= plant.ValueRO.MaxGrowth * manager.ReproductionCost * children;
                if (plant.ValueRW.Growth < 0f) plant.ValueRW.Growth = 0f;
                plant.ValueRW.Stage = PlantStage.Growing;
            }
        }

        foreach (var kvp in births)
        {
            if (kvp.Value >= manager.ReproductionThreshold && !occupancy.ContainsKey(kvp.Key))
            {
                var child = ecb.Instantiate(manager.Prefab);
                ecb.SetComponent(child, new LocalTransform
                {
                    Position = new float3(kvp.Key.x, 0f, kvp.Key.y),
                    Rotation = quaternion.identity,
                    Scale = 0.2f
                });
                ecb.SetComponent(child, new GridPosition { Cell = kvp.Key });
                ecb.SetComponent(child, new Plant
                {
                    Growth = prefabPlant.MaxGrowth * 0.2f,
                    MaxGrowth = prefabPlant.MaxGrowth,
                    GrowthRate = prefabPlant.GrowthRate,
                    ScaleStep = 1,
                    Stage = PlantStage.Growing
                });
                occupancy.TryAdd(kvp.Key, 0);
            }
        }

        ecb.Playback(state.EntityManager);
        occupancy.Dispose();
        births.Dispose();
    }
}
