using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct SpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (spawner, entity) in SystemAPI.Query<RefRO<Spawner>>().WithEntityAccess())
        {
            var prefab = spawner.ValueRO.Prefab;
            var count = spawner.ValueRO.Count;
            var rand = Unity.Mathematics.Random.CreateFromIndex(1);

            for (int i = 0; i < count; i++)
            {
                var e = ecb.Instantiate(prefab);
                ecb.SetComponent(e, new LocalTransform
                {
                    Position = new float3(rand.NextFloat(-50, 50), 0, rand.NextFloat(-50, 50)),
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                ecb.AddComponent(e, new Needs());
                ecb.AddComponent(e, new NeedTick { TimeUntilNext = rand.NextFloat(0f, 0.25f), Period = 0.25f });
                ecb.AddComponent(e, new GridPosition());
            }

            ecb.RemoveComponent<Spawner>(entity);
        }

        ecb.Playback(state.EntityManager);
    }
}
