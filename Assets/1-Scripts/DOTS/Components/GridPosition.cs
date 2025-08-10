using Unity.Entities;
using Unity.Mathematics;

/// Posición de la entidad dentro de una celda del grid.
public struct GridPosition : IComponentData
{
    /// Coordenadas de la celda ocupada por la entidad.
    public int2 Cell;
}

