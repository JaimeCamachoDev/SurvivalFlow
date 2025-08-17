using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Reproducción simple de herbívoros instanciando una cría cuando se dan las condiciones.
/// </summary>
[BurstCompile]
public partial struct HerbivoreReproductionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (intent, repro, meta, herb, health, transform, entity) in SystemAPI.Query<RefRO<Intent>, RefRW<Reproduction>, RefRW<Metabolism>, RefRW<Herbivore>, RefRO<Health>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            if (intent.ValueRO.State != 5)
                continue;
            if (repro.ValueRO.MateCooldown > 0f || health.ValueRO.Value < repro.ValueRO.MinHealth || meta.ValueRO.Energy < repro.ValueRO.MinEnergy)
                continue;

            meta.ValueRW.Energy = math.max(0f, meta.ValueRO.Energy - repro.ValueRO.MateCost);
            repro.ValueRW.MateCooldown = 120f; // reinicia

            var child = ecb.Instantiate(entity);
            ecb.SetComponent(child, new Herbivore
            {
                Generation = herb.ValueRO.Generation + 1,
                Age = 0f,
                LifeSpan = herb.ValueRO.LifeSpan
            });
            ecb.SetComponent(child, new Health { Value = 0.6f });
            ecb.SetComponent(child, new Metabolism
            {
                Energy = 0.4f,
                Stamina = 0.4f,
                Hunger = 0.6f,
                BaseRate = meta.ValueRO.BaseRate,
                MoveCost = meta.ValueRO.MoveCost,
                SprintCost = meta.ValueRO.SprintCost,
                HealThreshold = meta.ValueRO.HealThreshold,
                StarveThreshold = meta.ValueRO.StarveThreshold
            });
            ecb.SetComponent(child, transform.ValueRO);
        }
        ecb.Playback(state.EntityManager);
    }
}
