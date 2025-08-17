using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

/// Cambia el color de la planta según su estado.
[BurstCompile]
public partial struct PlantColorSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<VisualCueManager>(out var cues))
            return;

        foreach (var (plant, color) in SystemAPI.Query<RefRO<Plant>, RefRW<URPMaterialPropertyBaseColor>>())
        {
            float4 c = color.ValueRO.Value;
            switch (plant.ValueRO.Stage)
            {
                case PlantStage.Growing:
                    c = cues.PlantGrowingColor;
                    break;
                case PlantStage.Mature:
                    c = cues.PlantMatureColor;
                    break;
                case PlantStage.Withering:
                    c = cues.PlantWitheringColor;
                    break;
            }

            // Escribimos el nuevo color en el material.
            color.ValueRW.Value = c;
        }
    }
}
