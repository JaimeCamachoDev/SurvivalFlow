using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Instancia una entidad de carne cuando un herbívoro muere.
/// </summary>
[BurstCompile]
public partial struct HerbivoreDeathSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<HerbivoreManager>(out var manager))
            return;
        if (manager.MeatPrefab == Entity.Null)
            return;

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (health, gp, entity) in SystemAPI.Query<RefRO<Health>, RefRO<GridPosition>>().WithAll<Herbivore>().WithEntityAccess())
        {
            if (health.ValueRO.Value > 0f)
                continue;
            var drop = ecb.Instantiate(manager.MeatPrefab);
            ecb.SetComponent(drop, new LocalTransform
            {
                Position = new float3(gp.ValueRO.Cell.x, 0f, gp.ValueRO.Cell.y),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            ecb.AddComponent<MeatDropTag>(drop);
            ecb.DestroyEntity(entity);
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
