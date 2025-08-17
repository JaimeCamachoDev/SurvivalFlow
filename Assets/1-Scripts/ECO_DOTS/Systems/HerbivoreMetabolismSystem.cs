using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Actualiza energía, hambre y estamina de cada herbívoro.
/// </summary>
[BurstCompile]
public partial struct HerbivoreMetabolismSystem : ISystem
{
    const float K_HEAL = 0.02f;
    const float K_STARVE = 0.03f;

    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (meta, health, repro) in SystemAPI.Query<RefRW<Metabolism>, RefRW<Health>, RefRW<Reproduction>>())
        {
            meta.ValueRW.Energy = math.max(0f, meta.ValueRO.Energy - meta.ValueRO.BaseRate * dt);
            meta.ValueRW.Hunger = math.saturate(meta.ValueRO.Hunger + (meta.ValueRW.Energy - 0.5f) * dt);
            meta.ValueRW.Stamina = math.saturate(meta.ValueRO.Stamina + 0.25f * dt); // recarga lenta

            if (meta.ValueRO.Hunger >= meta.ValueRO.HealThreshold)
                health.ValueRW.Value = math.saturate(health.ValueRO.Value + K_HEAL * dt);
            if (meta.ValueRO.Hunger <= meta.ValueRO.StarveThreshold)
                health.ValueRW.Value = math.max(0f, health.ValueRO.Value - K_STARVE * dt);

            repro.ValueRW.MateCooldown = math.max(0f, repro.ValueRO.MateCooldown - dt);
        }
    }
}
