using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


/// Sistema que instancia plantas dentro de una cuadrícula evitando celdas ocupadas.
[BurstCompile]
public partial struct PlantSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (spawner, entity) in SystemAPI.Query<RefRO<PlantSpawner>>().WithEntityAccess())
        {
            var prefab = spawner.ValueRO.Prefab;
            var count = spawner.ValueRO.Count;
            var area = spawner.ValueRO.AreaSize;
            var patchCount = spawner.ValueRO.PatchCount;
            var patchRadius = spawner.ValueRO.PatchRadius;
            var rand = Unity.Mathematics.Random.CreateFromIndex(1);
            var prefabPlant = state.EntityManager.GetComponentData<Plant>(prefab);

            float minDist = 0f;
            if (SystemAPI.TryGetSingleton<PlantManager>(out var manager))
                minDist = manager.MinDistance;
            int minDistInt = (int)math.ceil(minDist);
            int2 half = (int2)(area / 2f);

            var used = new NativeParallelHashSet<int2>(count, Allocator.Temp);

            // Generate patch centers
            var centers = new NativeArray<float2>(patchCount, Allocator.Temp);
            for (int p = 0; p < patchCount; p++)
            {
                centers[p] = new float2(
                    rand.NextFloat(-area.x / 2f, area.x / 2f),
                    rand.NextFloat(-area.y / 2f, area.y / 2f));
            }

            int spawned = 0;
            int attempts = 0;
            int maxAttempts = count * 20;
            while (spawned < count && attempts < maxAttempts)
            {
                var center = centers[rand.NextInt(patchCount)];
                float2 offset = rand.NextFloat2Direction() * rand.NextFloat(0f, patchRadius);
                float2 pos = center + offset;
                int2 cell = new int2((int)math.round(pos.x), (int)math.round(pos.y));
                cell = math.clamp(cell, -half, half);
                attempts++;

                if (!IsFree(cell, ref used, minDistInt) || !used.Add(cell))
                    continue;

                var e = ecb.Instantiate(prefab);
                ecb.SetComponent(e, new LocalTransform
                {
                    Position = new float3(cell.x, 0f, cell.y),
                    Rotation = quaternion.identity,
                    Scale = 0.2f
                });

                prefabPlant.ScaleStep = 1;
                prefabPlant.Stage = PlantStage.Growing;
                ecb.SetComponent(e, prefabPlant);
                ecb.AddComponent(e, new GridPosition { Cell = cell });
                spawned++;
            }

            centers.Dispose();
            used.Dispose();

            ecb.RemoveComponent<PlantSpawner>(entity);
        }

        ecb.Playback(state.EntityManager);
    }

    static bool IsFree(int2 cell, ref NativeParallelHashSet<int2> occ, int minDist)
    {
        for (int x = -minDist; x <= minDist; x++)
            for (int y = -minDist; y <= minDist; y++)
                if (occ.Contains(cell + new int2(x, y)))
                    return false;
        return true;
    }
}
