using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Decreases hunger over time and destroys entities that starve.
/// </summary>
[BurstCompile]
public partial struct HungerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (hunger, entity) in SystemAPI.Query<RefRW<Hunger>>().WithEntityAccess())
        {
            hunger.ValueRW.Value -= hunger.ValueRO.DecreaseRate * dt;

            if (hunger.ValueRW.Value > hunger.ValueRO.Max)
            {
                hunger.ValueRW.Value = hunger.ValueRO.Max;
            }

            if (hunger.ValueRW.Value <= hunger.ValueRO.DeathThreshold)
            {
                ecb.DestroyEntity(entity);
            }
        }

        ecb.Playback(state.EntityManager);
    }
}

