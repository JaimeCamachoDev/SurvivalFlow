using Unity.Entities;

/// Buffer que almacena los costes de cada celda del *flow field*.
public struct FlowFieldCost : IBufferElementData
{
    /// Valor del coste de desplazamiento para una celda concreta.
    public float Value;
}
