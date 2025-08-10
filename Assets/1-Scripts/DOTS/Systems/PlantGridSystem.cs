using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Gestiona la reproducción y proliferación de plantas dentro de la cuadrícula.
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

        foreach (var (plant, gp) in SystemAPI.Query<RefRW<Plant>, RefRO<GridPosition>>())
        {
            if (plant.ValueRO.Stage != PlantStage.Mature)
                continue;

            for (int i = 0; i < manager.OffspringCount && current < limit; i++)
            {
                if (!TrySpawnAround(gp.ValueRO.Cell, manager.ReproductionRadius, ref occupancy, minDist, half, ref ecb, manager.Prefab, prefabPlant, ref rng))
                    break;

                current++;
                plant.ValueRW.Growth = math.max(0f, plant.ValueRO.Growth - manager.ReproductionCost);
                if (plant.ValueRW.Growth < plant.ValueRO.MaxGrowth)
                    plant.ValueRW.Stage = PlantStage.Growing;
            }
        }

        if (current < limit && (current == 0 || rng.NextFloat() < manager.RandomSpawnChance))
        {
            for (int attempt = 0; attempt < 10 && current < limit; attempt++)
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
        managerRw.ValueRW = manager;
    }

    static bool IsFree(int2 cell, ref NativeParallelHashSet<int2> occ, int minDist)
    {
        for (int x = -minDist; x <= minDist; x++)
            for (int y = -minDist; y <= minDist; y++)
                if (occ.Contains(cell + new int2(x, y)))
                    return false;
        return true;
    }

    static bool TrySpawnAround(int2 parent, int radius, ref NativeParallelHashSet<int2> occ, int minDist, int2 half,
        ref EntityCommandBuffer ecb, Entity prefab, in Plant template, ref Unity.Mathematics.Random rng)
    {
        var cells = new NativeList<int2>(Allocator.Temp);
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
                if (dx != 0 || dy != 0)
                    cells.Add(parent + new int2(dx, dy));

        for (int i = 0; i < cells.Length; i++)
        {
            int j = rng.NextInt(i, cells.Length);
            var tmp = cells[i];
            cells[i] = cells[j];
            cells[j] = tmp;
        }

        bool spawned = false;
        for (int i = 0; i < cells.Length; i++)
        {
            int2 cell = math.clamp(cells[i], -half, half);
            if (IsFree(cell, ref occ, minDist))
            {
                Spawn(cell, ref ecb, prefab, template);
                occ.Add(cell);
                spawned = true;
                break;
            }
        }

        cells.Dispose();
        return spawned;
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
