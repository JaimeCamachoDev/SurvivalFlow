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

        foreach (var gp in SystemAPI.Query<RefRO<GridPosition>>())
        {
            occupancy.TryAdd(gp.ValueRO.Cell, 0);
        }

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (plant, gp) in SystemAPI.Query<RefRW<Plant>, RefRO<GridPosition>>().WithEntityAccess())
        {
            int neighbours = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int2 check = gp.ValueRO.Cell + new int2(dx, dz);
                    if (occupancy.ContainsKey(check))
                        neighbours++;
                }
            }

            if (neighbours > manager.OvercrowdLimit)
            {
                plant.ValueRW.Stage = PlantStage.Withering;
            }

            if (plant.ValueRO.Stage == PlantStage.Mature)
            {
                bool reproduced = false;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int2 target = gp.ValueRO.Cell + new int2(dx, dz);
                        if (occupancy.ContainsKey(target))
                            continue;

                        var child = ecb.Instantiate(manager.Prefab);
                        ecb.SetComponent(child, new LocalTransform
                        {
                            Position = new float3(target.x, 0f, target.y),
                            Rotation = quaternion.identity,
                            Scale = 0.2f
                        });
                        ecb.SetComponent(child, new GridPosition { Cell = target });
                        ecb.SetComponent(child, new Plant
                        {
                            Growth = plant.ValueRO.MaxGrowth * 0.2f,
                            MaxGrowth = plant.ValueRO.MaxGrowth,
                            GrowthRate = plant.ValueRO.GrowthRate,
                            ScaleStep = 1,
                            Stage = PlantStage.Growing
                        });
                        occupancy.TryAdd(target, 0);
                        reproduced = true;
                    }
                }

                if (reproduced)
                {
                    plant.ValueRW.Growth -= plant.ValueRO.MaxGrowth * manager.ReproductionCost;
                    if (plant.ValueRW.Growth < 0f) plant.ValueRW.Growth = 0f;
                    plant.ValueRW.Stage = PlantStage.Growing;
                }
            }
            else if (plant.ValueRO.Stage == PlantStage.Withering && neighbours <= manager.OvercrowdLimit)
            {
                plant.ValueRW.Stage = PlantStage.Growing;
            }
        }

        ecb.Playback(state.EntityManager);
        occupancy.Dispose();
    }
}
