using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

/// Cambia el color de los herbívoros según su nivel de energía.
[BurstCompile]
public partial struct HerbivoreColorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<VisualCueManager>(out var cues))
            return;

        foreach (var (energy, color) in SystemAPI.Query<RefRO<Energy>, RefRW<URPMaterialPropertyBaseColor>>())
        {
            float4 c;
            if (energy.ValueRO.Value <= energy.ValueRO.SeekThreshold * 0.5f)
                c = cues.HerbivoreStarvingColor;
            else if (energy.ValueRO.Value <= energy.ValueRO.SeekThreshold)
                c = cues.HerbivoreHungryColor;
            else
                c = cues.HerbivoreNormalColor;

            color.ValueRW.Value = c;
        }
    }
}
