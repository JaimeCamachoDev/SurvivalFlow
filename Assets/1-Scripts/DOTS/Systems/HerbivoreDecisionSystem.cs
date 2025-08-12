using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Evalúa periódicamente el estado del herbívoro usando prioridades.
/// </summary>
[BurstCompile]
public partial struct HerbivoreDecisionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (hState, timer, hunger, repro, entity) in
                 SystemAPI.Query<RefRW<HerbivoreState>, RefRW<HerbivoreDecisionTimer>, RefRO<Hunger>, RefRO<Reproduction>>().WithEntityAccess())
        {
            timer.ValueRW.TimeLeft -= dt;
            if (timer.ValueRO.TimeLeft > 0f)
                continue;

            timer.ValueRW.TimeLeft = hState.ValueRO.DecisionCooldown;

            if (SystemAPI.HasComponent<Fleeing>(entity))
            {
                hState.ValueRW.Current = HerbivoreBehaviour.Flee;
                continue;
            }

            bool readyToMate = hunger.ValueRO.Value >= repro.ValueRO.Threshold && repro.ValueRO.Timer <= 0f;
            if (readyToMate)
            {
                hState.ValueRW.Current = HerbivoreBehaviour.Mate;
            }
            else if (hunger.ValueRO.Value < hunger.ValueRO.SeekThreshold)
            {
                hState.ValueRW.Current = HerbivoreBehaviour.Eat;
            }
            else
            {
                hState.ValueRW.Current = HerbivoreBehaviour.Wander;
            }
        }
    }
}
