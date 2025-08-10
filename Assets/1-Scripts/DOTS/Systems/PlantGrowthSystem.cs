using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

/// Actualiza el crecimiento y escala visual de cada planta.
[BurstCompile]
public partial struct PlantGrowthSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (plant, transform) in SystemAPI.Query<RefRW<Plant>, RefRW<LocalTransform>>())
        {
            if (plant.ValueRO.Growth < plant.ValueRO.MaxGrowth)
            {
                plant.ValueRW.Growth += plant.ValueRO.GrowthRate * dt;
                if (plant.ValueRW.Growth > plant.ValueRO.MaxGrowth)
                    plant.ValueRW.Growth = plant.ValueRO.MaxGrowth;
            }

            float scale = plant.ValueRO.Growth / plant.ValueRO.MaxGrowth;
            transform.ValueRW.Scale = scale;
        }
    }
}
