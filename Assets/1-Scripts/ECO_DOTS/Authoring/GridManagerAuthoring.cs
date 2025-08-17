using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// Autoría para administrar la cuadrícula del mundo.
public class GridManagerAuthoring : MonoBehaviour
{
    // Tamaño total del área del mundo.
    public Vector2 areaSize = new Vector2(50,50);

    // Tamaño de cada celda del grid.
    public float cellSize = 1f;

    // Dibuja un gizmo con la cuadrícula para visualizarla en el editor.
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

    // Convierte los datos del mono behaviour en componentes DOTS.
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

/// Datos ECS que definen la cuadrícula del mundo.
public struct GridManager : IComponentData
{
    /// Dimensiones del área total en celdas.
    public float2 AreaSize;

    /// Tamaño de cada celda.
    public float CellSize;
}
