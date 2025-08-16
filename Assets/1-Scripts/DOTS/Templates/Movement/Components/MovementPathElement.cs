using Unity.Entities;
using Unity.Mathematics;

/// Buffer que almacena el camino actual calculado para la plantilla de movimiento.
[InternalBufferCapacity(16)]
public struct MovementPathElement : IBufferElementData
{
    public int2 Cell;
}
