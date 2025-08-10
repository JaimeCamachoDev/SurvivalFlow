using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// Autoría para administrar la cuadrícula del mundo.
public class GridManagerAuthoring : MonoBehaviour
{
    public Vector2 areaSize = new Vector2(50,50);
    public float cellSize = 1f;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        for (float x = -areaSize.x * 0.5f; x <= areaSize.x * 0.5f; x += cellSize)
        {
            for (float y = -areaSize.y * 0.5f; y <= areaSize.y * 0.5f; y += cellSize)
            {
                Gizmos.DrawWireCube(new Vector3(x, 0f, y), new Vector3(cellSize, 0f, cellSize));
            }
        }
    }

    class Baker : Baker<GridManagerAuthoring>
    {
        public override void Bake(GridManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new GridManager
            {
                AreaSize = new float2(authoring.areaSize.x, authoring.areaSize.y),
                CellSize = authoring.cellSize
            });
        }
    }
}

public struct GridManager : IComponentData
{
    public float2 AreaSize;
    public float CellSize;
}
