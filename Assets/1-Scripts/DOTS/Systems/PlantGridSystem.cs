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
        int limit = manager.EnforceDensity
            ? (int)math.round(manager.Density * manager.MaxPlants)
            : manager.MaxPlants;
        // Reserve capacity up to the population limit so new births fit without errors
        var occupancy = new NativeParallelHashSet<int2>(limit, Allocator.Temp);

        foreach (var gp in SystemAPI.Query<RefRO<GridPosition>>())
        {
            occupancy.Add(gp.ValueRO.Cell);
        }

        // limit computed earlier

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var prefabPlant = state.EntityManager.GetComponentData<Plant>(manager.Prefab);
        int births = 0;
        if (current < limit)
        {
            foreach (var (plant, gp) in SystemAPI.Query<RefRO<Plant>, RefRO<GridPosition>>())
            {
                if (plant.ValueRO.Stage != PlantStage.Mature || current >= limit || births >= manager.ReproductionThreshold)
                    continue;

                var rnd = Unity.Mathematics.Random.CreateFromIndex((uint)math.hash(gp.ValueRO.Cell));
                for (int i = 0; i < 8; i++)
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
                        // Newly spawned plants need a GridPosition component
                        ecb.AddComponent(child, new GridPosition { Cell = cell });
                        ecb.SetComponent(child, new Plant
                        {
                            Growth = prefabPlant.MaxGrowth * 0.2f,
                            MaxGrowth = prefabPlant.MaxGrowth,
                            GrowthRate = prefabPlant.GrowthRate,
                            ScaleStep = 1,
                            Stage = PlantStage.Growing
                        });
                        current++;
                        births++;
                        break;
                    }
                }
            }
        }

        if (current > limit)
        {
            var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < current - limit && i < entities.Length; i++)
            {
                ecb.DestroyEntity(entities[i]);
            }
            entities.Dispose();
        }

        ecb.Playback(state.EntityManager);
        occupancy.Dispose();
    }
}
