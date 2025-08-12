using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Mueve a los herbívoros que están huyendo, ignorando otras conductas.
/// </summary>
[BurstCompile]
public partial struct FleeingMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (transform, herb, gp, fleeing, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<Herbivore>, RefRW<GridPosition>, RefRW<Fleeing>>().WithEntityAccess())
        {
            float speed = herb.ValueRO.MoveSpeed * 2f;
            transform.ValueRW.Position += fleeing.ValueRO.Direction * speed * dt;
            fleeing.ValueRW.TimeLeft -= dt;
            gp.ValueRW.Cell = new int2((int)math.round(transform.ValueRO.Position.x / grid.CellSize),
                                       (int)math.round(transform.ValueRO.Position.z / grid.CellSize));
            if (fleeing.ValueRO.TimeLeft <= 0f)
            {
                state.EntityManager.RemoveComponent<Fleeing>(entity);
            }
        }
    }
}
