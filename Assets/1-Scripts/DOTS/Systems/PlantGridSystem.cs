using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Gestiona la población de plantas dentro de la cuadrícula.
[BurstCompile]
public partial struct PlantGridSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonRW<PlantManager>(out var managerRw))
            return;

        var manager = managerRw.ValueRO;
        manager.Timer += SystemAPI.Time.DeltaTime;
        if (manager.Timer < manager.ReproductionInterval)
        {
            managerRw.ValueRW = manager;
            return;
        }
        manager.Timer = 0f;

        var query = SystemAPI.QueryBuilder().WithAll<Plant, GridPosition>().Build();
        int current = query.CalculateEntityCount();
        int limit = manager.MaxPlants;

        var occupancy = new NativeParallelHashSet<int2>(limit, Allocator.Temp);
        foreach (var gp in SystemAPI.Query<RefRO<GridPosition>>())
            occupancy.Add(gp.ValueRO.Cell);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var prefabPlant = state.EntityManager.GetComponentData<Plant>(manager.Prefab);
        int2 half = (int2)(manager.AreaSize / 2f);
        int minDist = (int)math.ceil(manager.MinDistance);

        var rng = Unity.Mathematics.Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000 + 1));

        if (current < limit && rng.NextFloat() < manager.RandomSpawnChance)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                int2 cell = new int2(
                    rng.NextInt(-half.x, half.x),
                    rng.NextInt(-half.y, half.y));

                if (IsFree(cell, ref occupancy, minDist))
                {
                    Spawn(cell, ref ecb, manager.Prefab, prefabPlant);
                    occupancy.Add(cell);
                    current++;
                    break;
                }
            }
        }

        if (current > limit)
        {
            var entities = query.ToEntityArray(Allocator.Temp);
            int remove = math.min(current - limit, entities.Length);
            for (int i = 0; i < remove; i++)
                ecb.DestroyEntity(entities[i]);
            entities.Dispose();
        }

        ecb.Playback(state.EntityManager);
        occupancy.Dispose();
    }

    static bool IsFree(int2 cell, ref NativeParallelHashSet<int2> occ, int minDist)
    {
        for (int x = -minDist; x <= minDist; x++)
            for (int y = -minDist; y <= minDist; y++)
                if (occ.Contains(cell + new int2(x, y)))
                    return false;
        return true;
    }

    static void Spawn(int2 cell, ref EntityCommandBuffer ecb, Entity prefab, in Plant template)
    {
        var e = ecb.Instantiate(prefab);
        ecb.SetComponent(e, new LocalTransform
        {
            Position = new float3(cell.x, 0f, cell.y),
            Rotation = quaternion.identity,
            Scale = 0.2f
        });
        var plant = template;
        plant.Growth = template.MaxGrowth * 0.2f;
        plant.ScaleStep = 1;
        plant.Stage = PlantStage.Growing;
        ecb.SetComponent(e, plant);
        ecb.AddComponent(e, new GridPosition { Cell = cell });
    }
}
