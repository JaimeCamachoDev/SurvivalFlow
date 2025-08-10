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
            var rand = Unity.Mathematics.Random.CreateFromIndex(1);
            var prefabPlant = state.EntityManager.GetComponentData<Plant>(prefab);
            var used = new NativeParallelHashSet<int2>(count, Allocator.Temp);

            int2 half = (int2)(area / 2f);
            for (int i = 0; i < count; i++)
            {
                int2 cell;
                do
                {
                    cell = new int2(
                        rand.NextInt(-half.x, half.x),
                        rand.NextInt(-half.y, half.y));
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

            used.Dispose();
            ecb.RemoveComponent<PlantSpawner>(entity);
        }

        ecb.Playback(state.EntityManager);
    }
}
