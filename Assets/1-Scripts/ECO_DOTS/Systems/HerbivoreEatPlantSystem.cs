using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Maneja el mordisco de plantas cuando el herbívoro está en estado de comer.
/// </summary>
[BurstCompile]
public partial struct HerbivoreEatPlantSystem : ISystem
{
    const float BITE_SIZE = 5f;
    const float K_NUTRITION = 0.01f;
    const float K_ENERGY = 0.02f;
    const float K_STAMINA = 0.03f;

    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (intent, meta, target, hTransform) in SystemAPI.Query<RefRO<Intent>, RefRW<Metabolism>, RefRO<TargetPlant>, RefRO<LocalTransform>>())
        {
            if (intent.ValueRO.State != 2 || target.ValueRO.Plant == Entity.Null)
                continue;

            if (!SystemAPI.HasComponent<Plant>(target.ValueRO.Plant) || !SystemAPI.HasComponent<LocalTransform>(target.ValueRO.Plant))
                continue;

            var plant = SystemAPI.GetComponentRW<Plant>(target.ValueRO.Plant);
            var pTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.Plant);
            float dist = math.distance(hTransform.ValueRO.Position, pTransform.Position);
            if (dist > 0.6f)
                continue;

            float bite = BITE_SIZE * dt;
            plant.ValueRW.BeingEaten = 1;
            plant.ValueRW.Stage = PlantStage.Withering;
            plant.ValueRW.Energy = math.max(0f, plant.ValueRO.Energy - bite);

            meta.ValueRW.Hunger = math.saturate(meta.ValueRO.Hunger + bite * K_NUTRITION);
            meta.ValueRW.Energy = math.saturate(meta.ValueRO.Energy + bite * K_ENERGY);
            meta.ValueRW.Stamina = math.saturate(meta.ValueRO.Stamina + bite * K_STAMINA);

            if (plant.ValueRW.Energy <= 0f)
                SystemAPI.SetComponent(target.ValueRO.Plant, plant.ValueRW);
        }
    }
}
