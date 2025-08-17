using Unity.Entities;
using Unity.Mathematics;

/// Ruta almacenada para que un agente pueda recorrerla sin recalcular cada frame.
[InternalBufferCapacity(16)]
public struct PathBufferElement : IBufferElementData
{
    /// Celda de la ruta que debe seguir el agente.
    public int2 Cell;
}
