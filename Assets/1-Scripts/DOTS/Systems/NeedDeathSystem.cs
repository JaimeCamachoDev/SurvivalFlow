using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
public partial struct NeedDeathSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (needs, entity) in SystemAPI.Query<RefRO<Needs>>().WithEntityAccess())
        {
            if (needs.ValueRO.Hunger >= 1f || needs.ValueRO.Thirst >= 1f || needs.ValueRO.Sleep >= 1f)
            {
                ecb.DestroyEntity(entity);
            }
        }

        ecb.Playback(state.EntityManager);
    }
}
