using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct GridUpdateSystem : ISystem
{
    public const int CELL_SIZE = 2;

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (gridPos, localToWorld) in
                 SystemAPI.Query<RefRW<GridPosition>, RefRO<LocalToWorld>>())
        {
            float3 pos = localToWorld.ValueRO.Position;
            int2 cell = (int2)math.floor(pos.xz / CELL_SIZE);
            gridPos.ValueRW.Cell = cell;
        }
    }
}
