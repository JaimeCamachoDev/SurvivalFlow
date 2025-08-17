using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Busca plantas cercanas para cada herbívoro dentro de su radio de visión.
/// </summary>
[BurstCompile]
public partial struct HerbivoreScanSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var plantQuery = SystemAPI.QueryBuilder().WithAll<Plant, LocalTransform>().Build();
        var plantEntities = plantQuery.ToEntityArray(Allocator.Temp);
        var plantTransforms = plantQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var plantData = plantQuery.ToComponentDataArray<Plant>(Allocator.Temp);

        foreach (var (loc, transform, target, entity) in SystemAPI.Query<RefRO<Locomotion>, RefRO<LocalTransform>, RefRW<TargetPlant>>().WithEntityAccess())
        {
            float bestDistSq = loc.ValueRO.VisionRadius * loc.ValueRO.VisionRadius;
            Entity best = Entity.Null;
            float3 pos = transform.ValueRO.Position;

            for (int i = 0; i < plantEntities.Length; i++)
            {
                var plant = plantData[i];
                if (plant.Stage == PlantStage.Withering || plant.BeingEaten != 0)
                    continue;
                float3 pPos = plantTransforms[i].Position;
                float distSq = math.distancesq(pos, pPos);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = plantEntities[i];
                }
            }

            target.ValueRW.Plant = best;
        }

        plantEntities.Dispose();
        plantTransforms.Dispose();
        plantData.Dispose();
    }
}
