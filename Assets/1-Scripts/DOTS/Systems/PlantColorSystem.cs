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
        foreach (var (plant, color) in SystemAPI.Query<RefRO<Plant>, RefRW<URPMaterialPropertyBaseColor>>())
        {
            float4 c = color.ValueRO.Value;
            switch (plant.ValueRO.Stage)
            {
                case PlantStage.Growing:
                    c = new float4(1f, 1f, 0f, 1f); // Amarillo
                    break;
                case PlantStage.Mature:
                    c = new float4(0f, 1f, 0f, 1f); // Verde
                    break;
                case PlantStage.Withering:
                    c = new float4(1f, 0f, 0f, 1f); // Rojo
                    break;
            }

            // Escribimos el nuevo color en el material.
            color.ValueRW.Value = c;
        }
    }
}
