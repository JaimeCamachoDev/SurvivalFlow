using Unity.Entities;
using Unity.Mathematics;

/// Posición de la entidad dentro de una celda del grid.
public struct GridPosition : IComponentData
{
    public int2 Cell;
}
