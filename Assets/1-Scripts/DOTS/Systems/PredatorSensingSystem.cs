using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Detecta depredadores cercanos y activa la huida de los herbívoros.
/// </summary>
[BurstCompile]
public partial struct PredatorSensingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var predators = new NativeList<float3>(Allocator.Temp);
        foreach (var (t, _) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<CarnivoreTag>())
        {
            predators.Add(t.ValueRO.Position);
        }

        foreach (var (t, sense, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PredatorSense>>().WithEntityAccess())
        {
            float3 escape = float3.zero;
            bool seen = false;
            for (int i = 0; i < predators.Length; i++)
            {
                float3 diff = t.ValueRO.Position - predators[i];
                float distSq = math.lengthsq(diff);
                if (distSq <= sense.ValueRO.Radius * sense.ValueRO.Radius)
                {
                    escape += math.normalize(diff);
                    seen = true;
                }
            }

            if (seen)
            {
                var fleeing = new Fleeing { TimeLeft = 3f, Direction = math.normalize(escape) };
                if (SystemAPI.HasComponent<Fleeing>(entity))
                    SystemAPI.SetComponent(entity, fleeing);
                else
                    state.EntityManager.AddComponentData(entity, fleeing);
            }
        }
        predators.Dispose();
    }
}
