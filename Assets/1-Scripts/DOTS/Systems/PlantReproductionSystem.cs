using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Sistema que permite a las plantas maduras reproducirse en celdas adyacentes.
[BurstCompile]
public partial struct PlantReproductionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<PlantManager>(out var manager) ||
            !SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var occupancy = new NativeParallelHashSet<int2>(128, Allocator.Temp);

        foreach (var gp in SystemAPI.Query<RefRO<GridPosition>>())
            occupancy.Add(gp.ValueRO.Cell);

        var rand = Unity.Mathematics.Random.CreateFromIndex(
            (uint)(SystemAPI.Time.ElapsedTime * 1000 + 1));

        int2[] dirs = new int2[8]
        {
            new int2(1,0), new int2(-1,0), new int2(0,1), new int2(0,-1),
            new int2(1,1), new int2(1,-1), new int2(-1,1), new int2(-1,-1)
        };

        float2 half = grid.AreaSize * 0.5f;

        foreach (var (plant, gp, entity) in SystemAPI.Query<RefRW<Plant>, RefRO<GridPosition>>().WithEntityAccess())
        {
            if (plant.ValueRO.Stage != PlantStage.Mature)
                continue;

            float cost = manager.ReproductionCost * plant.ValueRO.MaxGrowth;
            if (plant.ValueRO.Growth < cost)
                continue;

            var available = new NativeList<int2>(Allocator.Temp);
            foreach (var dir in dirs)
            {
                int2 cell = gp.ValueRO.Cell + dir;
                if (cell.x < -half.x || cell.x > half.x || cell.y < -half.y || cell.y > half.y)
                    continue;
                if (!occupancy.Contains(cell))
                    available.Add(cell);
            }

            int spawnAttempts = math.min(manager.ReproductionCount, available.Length);
            bool reproduced = false;

            for (int i = 0; i < spawnAttempts && plant.ValueRW.Growth >= cost; i++)
            {
                int index = rand.NextInt(available.Length);
                int2 target = available[index];
                available.RemoveAtSwapBack(index);

                occupancy.Add(target);

                var child = ecb.Instantiate(manager.Prefab);
                ecb.SetComponent(child, new LocalTransform
                {
                    Position = new float3(target.x, 0f, target.y),
                    Rotation = quaternion.identity,
                    Scale = 0.2f
                });

                var template = state.EntityManager.GetComponentData<Plant>(manager.Prefab);
                template.ScaleStep = 1;
                template.Stage = PlantStage.Growing;
                ecb.SetComponent(child, template);
                ecb.AddComponent(child, new GridPosition { Cell = target });

                plant.ValueRW.Growth -= cost;
                reproduced = true;
            }

            available.Dispose();

            if (reproduced)
                plant.ValueRW.Stage = PlantStage.Growing;
        }

        ecb.Playback(state.EntityManager);
        occupancy.Dispose();
    }
}
