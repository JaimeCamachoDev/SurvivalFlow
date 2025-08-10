using Unity.Entities;
using Unity.Mathematics;

/// Posici�n de la entidad dentro de una celda del grid.
public struct GridPosition : IComponentData
{
    public int2 Cell;
}
