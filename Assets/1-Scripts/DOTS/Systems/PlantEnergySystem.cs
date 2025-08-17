using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Actualiza la energía y la escala visual de cada planta.
[BurstCompile]
public partial struct PlantEnergySystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Recorremos todas las plantas para actualizar su energía.
        foreach (var (plant, transform, entity) in SystemAPI.Query<RefRW<Plant>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            bool eaten = plant.ValueRO.BeingEaten != 0;
            var stage = plant.ValueRO.Stage;
            if (stage == PlantStage.Withering && !eaten && plant.ValueRO.Energy > 0f)
            {
                stage = PlantStage.Growing;
                plant.ValueRW.Stage = PlantStage.Growing;
            }

            switch (stage)
            {
                case PlantStage.Growing:
                    // Aumenta la energía hasta alcanzar el máximo.
                    plant.ValueRW.Energy += plant.ValueRO.EnergyGainRate * dt;
                    if (plant.ValueRW.Energy >= plant.ValueRO.MaxEnergy)
                    {
                        plant.ValueRW.Energy = plant.ValueRW.MaxEnergy;
                        plant.ValueRW.Stage = PlantStage.Mature;
                    }
                    break;
                case PlantStage.Withering:
                    // La reducción de energía ocurre al ser consumida por herbívoros.
                    if (plant.ValueRO.Energy <= 0f)
                    {
                        ecb.DestroyEntity(entity);
                        continue;
                    }
                    break;
            }

            plant.ValueRW.BeingEaten = 0;

            // Ajusta la escala visual de forma continua según la energía.
            float percent = math.clamp(plant.ValueRO.Energy / plant.ValueRO.MaxEnergy, 0f, 1f);
            transform.ValueRW.Scale = percent;
        }

        ecb.Playback(state.EntityManager);
    }
}
