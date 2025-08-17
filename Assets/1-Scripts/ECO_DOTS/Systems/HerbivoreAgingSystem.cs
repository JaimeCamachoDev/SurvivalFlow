using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Incrementa la edad de los herbívoros y elimina los que mueren.
/// </summary>
[BurstCompile]
public partial struct HerbivoreAgingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (herb, health, entity) in SystemAPI.Query<RefRW<Herbivore>, RefRO<Health>>().WithEntityAccess())
        {
            herb.ValueRW.Age += dt;
            if (herb.ValueRO.Age >= herb.ValueRO.LifeSpan || health.ValueRO.Value <= 0f)
                ecb.DestroyEntity(entity);
        }
        ecb.Playback(state.EntityManager);
    }
}
