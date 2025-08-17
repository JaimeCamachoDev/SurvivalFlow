using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Calcula un estado deseado simple para cada herbívoro usando utilidades básicas.
/// </summary>
[BurstCompile]
public partial struct HerbivoreDecideIntentSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (meta, repro, health, intent, target, entity) in SystemAPI.Query<RefRO<Metabolism>, RefRW<Reproduction>, RefRO<Health>, RefRW<Intent>, RefRW<TargetPlant>>().WithEntityAccess())
        {
            float U_Eat = target.ValueRO.Plant != Entity.Null ? math.max(0f, 1f - meta.ValueRO.Hunger) : 0f;
            float U_Mate = repro.ValueRO.Fertility * math.step(repro.ValueRO.MinHealth, health.ValueRO.Value) *
                           math.step(repro.ValueRO.MinEnergy, meta.ValueRO.Energy) *
                           math.step(0f, -repro.ValueRO.MateCooldown);
            float U_Forage = math.step(meta.ValueRO.Energy, 0.5f);
            float U_Rest = math.step(0.2f, meta.ValueRO.Stamina) * math.step(0.7f, meta.ValueRO.Hunger) * math.step(0.6f, meta.ValueRO.Energy);
            float U_Roam = 0.1f;

            byte stateId = 0; // Idle
            float best = U_Roam;
            stateId = 4; // roam por defecto

            if (U_Forage > best)
            { best = U_Forage; stateId = 1; }
            if (U_Eat > best)
            { best = U_Eat; stateId = 2; }
            if (U_Mate > best)
            { best = U_Mate; stateId = 5; }
            if (U_Rest > best)
            { best = U_Rest; stateId = 6; }

            intent.ValueRW.State = stateId;
        }
    }
}
