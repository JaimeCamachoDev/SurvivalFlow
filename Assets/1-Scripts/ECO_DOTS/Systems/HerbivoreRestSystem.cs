using Unity.Burst;
using Unity.Entities;

/// <summary>
/// Regenera estamina y algo de energía cuando el herbívoro descansa.
/// </summary>
[BurstCompile]
public partial struct HerbivoreRestSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (intent, meta) in SystemAPI.Query<RefRO<Intent>, RefRW<Metabolism>>())
        {
            if (intent.ValueRO.State != 6)
                continue;
            meta.ValueRW.Stamina = math.saturate(meta.ValueRO.Stamina + 0.5f * dt);
            meta.ValueRW.Energy = math.saturate(meta.ValueRO.Energy + 0.1f * dt);
        }
    }
}
