using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Actualiza el crecimiento y la escala visual de cada planta.
[BurstCompile]
public partial struct PlantGrowthSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Recorremos todas las plantas para actualizar su crecimiento.
        foreach (var (plant, transform, entity) in SystemAPI.Query<RefRW<Plant>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            switch (plant.ValueRO.Stage)
            {
                case PlantStage.Growing:
                    // Aumenta el tamaño hasta alcanzar el máximo.
                    plant.ValueRW.Growth += plant.ValueRO.GrowthRate * dt;
                    if (plant.ValueRW.Growth >= plant.ValueRO.MaxGrowth)
                    {
                        plant.ValueRW.Growth = plant.ValueRO.MaxGrowth;
                        plant.ValueRW.Stage = PlantStage.Mature;
                    }
                    break;
                case PlantStage.Withering:
                    // La reducción de tamaño ocurre al ser consumida por herbívoros.
                    if (plant.ValueRO.Growth <= 0f)
                    {
                        ecb.DestroyEntity(entity);
                        continue;
                    }
                    break;
            }

            // Ajusta la escala visual solo cuando cambia lo suficiente.
            float percent = math.clamp(plant.ValueRO.Growth / plant.ValueRO.MaxGrowth, 0f, 1f);
            int step = (int)math.floor(percent * 10f);
            if (step < 1) step = 1;
            if (plant.ValueRO.ScaleStep != step)
            {
                transform.ValueRW.Scale = step / 10f;
                plant.ValueRW.ScaleStep = (byte)step;
            }
        }

        ecb.Playback(state.EntityManager);
    }
}
