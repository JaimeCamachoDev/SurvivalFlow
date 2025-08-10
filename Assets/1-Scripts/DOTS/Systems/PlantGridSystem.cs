using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Gestiona la proliferación de plantas controlando la densidad global.
[BurstCompile]
public partial struct PlantGridSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<PlantManager>(out var manager))
            return;

        var query = SystemAPI.QueryBuilder().WithAll<Plant, GridPosition>().Build();
        int current = query.CalculateEntityCount();
        var occupancy = new NativeParallelHashSet<int2>(current, Allocator.Temp);

        foreach (var gp in SystemAPI.Query<RefRO<GridPosition>>())
        {
            occupancy.Add(gp.ValueRO.Cell);
        }

        int target = manager.EnforceDensity ? (int)math.round(manager.Density * manager.MaxPlants) : int.MaxValue;
        int neededBirths = math.max(0, target - current);
        int neededDeaths = math.max(0, current - target);

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var prefabPlant = state.EntityManager.GetComponentData<Plant>(manager.Prefab);

        if (neededBirths > 0)
        {
            foreach (var (plant, gp) in SystemAPI.Query<RefRO<Plant>, RefRO<GridPosition>>())
            {
                if (plant.ValueRO.Stage != PlantStage.Mature)
                    continue;

                var rnd = Unity.Mathematics.Random.CreateFromIndex((uint)math.hash(gp.ValueRO.Cell));
                for (int i = 0; i < 8 && neededBirths > 0; i++)
                {
                    int2 offset;
                    do
                    {
                        offset = new int2(rnd.NextInt(-1, 2), rnd.NextInt(-1, 2));
                    }
                    while (offset.x == 0 && offset.y == 0);

                    int2 cell = gp.ValueRO.Cell + offset;
                    if (occupancy.Add(cell))
                    {
                        var child = ecb.Instantiate(manager.Prefab);
                        ecb.SetComponent(child, new LocalTransform
                        {
                            Position = new float3(cell.x, 0f, cell.y),
                            Rotation = quaternion.identity,
                            Scale = 0.2f
                        });
                        ecb.SetComponent(child, new GridPosition { Cell = cell });
                        ecb.SetComponent(child, new Plant
                        {
                            Growth = prefabPlant.MaxGrowth * 0.2f,
                            MaxGrowth = prefabPlant.MaxGrowth,
                            GrowthRate = prefabPlant.GrowthRate,
                            ScaleStep = 1,
                            Stage = PlantStage.Growing
                        });
                        neededBirths--;
                        break;
                    }
                }

                if (neededBirths == 0)
                    break;
            }
        }

        if (neededDeaths > 0)
        {
            var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < neededDeaths && i < entities.Length; i++)
            {
                ecb.DestroyEntity(entities[i]);
            }
            entities.Dispose();
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
