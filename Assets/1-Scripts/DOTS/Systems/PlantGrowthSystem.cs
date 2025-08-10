using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Actualiza el crecimiento y escala visual de cada planta.
[BurstCompile]
public partial struct PlantGrowthSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (plant, transform, entity) in SystemAPI.Query<RefRW<Plant>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            switch (plant.ValueRO.Stage)
            {
                case PlantStage.Growing:
                    plant.ValueRW.Growth += plant.ValueRO.GrowthRate * dt;
                    if (plant.ValueRW.Growth >= plant.ValueRO.MaxGrowth)
                    {
                        plant.ValueRW.Growth = plant.ValueRO.MaxGrowth;
                        plant.ValueRW.Stage = PlantStage.Mature;
                    }
                    break;
                case PlantStage.Withering:
                    plant.ValueRW.Growth -= plant.ValueRO.GrowthRate * dt;
                    if (plant.ValueRO.Growth <= 0f)
                    {
                        ecb.DestroyEntity(entity);
                        continue;
                    }
                    break;
            }

            float percent = math.clamp(plant.ValueRO.Growth / plant.ValueRO.MaxGrowth, 0f, 1f);
            int step = (int)math.floor(percent * 5f);
            if (step < 1) step = 1;
            if (plant.ValueRO.ScaleStep != step)
            {
                transform.ValueRW.Scale = step / 5f;
                plant.ValueRW.ScaleStep = (byte)step;
            }
        }

        ecb.Playback(state.EntityManager);
    }
}
