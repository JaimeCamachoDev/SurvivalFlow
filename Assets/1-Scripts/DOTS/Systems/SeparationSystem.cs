using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Aplica una fuerza de repulsión entre herbívoros cercanos para evitar apelotonamientos.
/// </summary>
[BurstCompile]
public partial struct SeparationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var positions = new NativeList<float3>(Allocator.Temp);
        var entities = new NativeList<Entity>(Allocator.Temp);
        foreach (var (t, e) in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Herbivore>().WithEntityAccess())
        {
            positions.Add(t.ValueRO.Position);
            entities.Add(e);
        }

        float dt = SystemAPI.Time.DeltaTime;
        for (int i = 0; i < entities.Length; i++)
        {
            var ent = entities[i];
            if (!SystemAPI.HasComponent<SeparationRadius>(ent))
                continue;
            var sep = SystemAPI.GetComponent<SeparationRadius>(ent);
            float3 pos = positions[i];
            float3 force = float3.zero;
            for (int j = 0; j < entities.Length; j++)
            {
                if (i == j) continue;
                float3 diff = pos - positions[j];
                float dist = math.length(diff);
                if (dist > 0f && dist < sep.Value)
                {
                    force += math.normalize(diff) * (sep.Force / dist);
                }
            }
            if (!force.Equals(float3.zero))
            {
                var herb = SystemAPI.GetComponent<Herbivore>(ent);
                herb.MoveDirection = math.normalize(herb.MoveDirection + force * dt);
                SystemAPI.SetComponent(ent, herb);
            }
        }
        positions.Dispose();
        entities.Dispose();
    }
}
