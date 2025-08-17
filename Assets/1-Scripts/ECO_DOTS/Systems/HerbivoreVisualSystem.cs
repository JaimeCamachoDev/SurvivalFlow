using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// Ajusta color y escala de los herbívoros según su estado y salud.
/// </summary>
[BurstCompile]
public partial struct HerbivoreVisualSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (hState, health, color, transform, sColor) in
                 SystemAPI.Query<RefRO<HerbivoreState>, RefRO<Health>, RefRW<URPMaterialPropertyBaseColor>, RefRW<LocalTransform>, RefRO<StateColor>>())
        {
            float4 c = sColor.ValueRO.Wander;
            switch (hState.ValueRO.Current)
            {
                case HerbivoreBehaviour.Eat: c = sColor.ValueRO.Eat; break;
                case HerbivoreBehaviour.Mate: c = sColor.ValueRO.Mate; break;
                case HerbivoreBehaviour.Flee: c = sColor.ValueRO.Flee; break;
            }
            color.ValueRW.Value = c;
            float scale = math.max(0.3f, health.ValueRO.Value / health.ValueRO.Max);
            transform.ValueRW.Scale = scale;
        }
    }
}
