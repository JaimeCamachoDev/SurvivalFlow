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
            var used = new NativeParallelHashSet<int2>(count, Allocator.Temp);

            // Generate patch centers
            var centers = new NativeArray<float2>(patchCount, Allocator.Temp);
            for (int p = 0; p < patchCount; p++)
            {
                centers[p] = new float2(
                    rand.NextFloat(-area.x / 2f, area.x / 2f),
                    rand.NextFloat(-area.y / 2f, area.y / 2f));
            }

            for (int i = 0; i < count; i++)
            {
                int2 cell;
                do
                {
                    var center = centers[rand.NextInt(patchCount)];
                    float2 offset = rand.NextFloat2Direction() * rand.NextFloat(0f, patchRadius);
                    float2 pos = center + offset;
                    cell = new int2((int)math.round(pos.x), (int)math.round(pos.y));
                } while (!used.Add(cell));

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
            }

            centers.Dispose();
            used.Dispose();

            ecb.RemoveComponent<PlantSpawner>(entity);
        }

        ecb.Playback(state.EntityManager);
    }
}
