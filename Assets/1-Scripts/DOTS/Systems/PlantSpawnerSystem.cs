using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Sistema que instancia muchas plantas en posiciones aleatorias dentro de un área.
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
            var prefabTransform = state.EntityManager.GetComponentData<LocalTransform>(prefab);

            for (int i = 0; i < count; i++)
            {
                var e = ecb.Instantiate(prefab);
                var pos = new float3(
                    rand.NextFloat(-area.x / 2f, area.x / 2f),
                    0f,
                    rand.NextFloat(-area.y / 2f, area.y / 2f));
                ecb.SetComponent(e, new LocalTransform
                {
                    Position = pos,
                    Rotation = quaternion.identity,
                    Scale = prefabTransform.Scale
                });
            }

            ecb.RemoveComponent<PlantSpawner>(entity);
        }

        ecb.Playback(state.EntityManager);
    }
}
