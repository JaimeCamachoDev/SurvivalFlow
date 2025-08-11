using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

/// Cambia el color de los herbívoros según su nivel de hambre.
[BurstCompile]
public partial struct HerbivoreColorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<VisualCueManager>(out var cues))
            return;

        foreach (var (hunger, color) in SystemAPI.Query<RefRO<Hunger>, RefRW<URPMaterialPropertyBaseColor>>())
        {
            float4 c;
            if (hunger.ValueRO.Value <= hunger.ValueRO.SeekThreshold * 0.5f)
                c = cues.HerbivoreStarvingColor;
            else if (hunger.ValueRO.Value <= hunger.ValueRO.SeekThreshold)
                c = cues.HerbivoreHungryColor;
            else
                c = cues.HerbivoreNormalColor;

            color.ValueRW.Value = c;
        }
    }
}
