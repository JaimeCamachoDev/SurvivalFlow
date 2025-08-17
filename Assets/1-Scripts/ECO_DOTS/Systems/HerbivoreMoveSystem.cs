using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Mueve a los herbívoros según su intención actual.
/// </summary>
[BurstCompile]
public partial struct HerbivoreMoveSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (loc, intent, transform, target, entity) in SystemAPI.Query<RefRO<Locomotion>, RefRO<Intent>, RefRW<LocalTransform>, RefRO<TargetPlant>>().WithEntityAccess())
        {
            float3 pos = transform.ValueRO.Position;
            float3 dest = pos;
            float speed = loc.ValueRO.WalkSpeed;

            if ((intent.ValueRO.State == 1 || intent.ValueRO.State == 2 || intent.ValueRO.State == 5) && target.ValueRO.Plant != Entity.Null)
            {
                if (SystemAPI.HasComponent<LocalTransform>(target.ValueRO.Plant))
                {
                    dest = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.Plant).Position;
                }
            }
            else if (intent.ValueRO.State == 4)
            {
                // Pequeño desplazamiento aleatorio para vagar.
                dest = pos + new float3(math.sin(pos.x + pos.z), 0f, math.cos(pos.x - pos.z));
            }
            else
            {
                continue; // Idle u otros estados sin movimiento
            }

            float3 dir = dest - pos;
            if (math.lengthsq(dir) > 0.0001f)
            {
                dir = math.normalize(dir);
                transform.ValueRW.Position = pos + dir * speed * dt;
            }
        }
    }
}
