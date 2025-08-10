using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct GridUpdateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        float cellSize = grid.CellSize;

        foreach (var (gridPos, localToWorld) in
                 SystemAPI.Query<RefRW<GridPosition>, RefRO<LocalToWorld>>())
        {
            float3 pos = localToWorld.ValueRO.Position;
            int2 cell = (int2)math.floor(pos.xz / cellSize);
            gridPos.ValueRW.Cell = cell;
        }
    }
}
