using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Colores usados para visualizar el estado del herbívoro.
/// </summary>
public struct StateColor : IComponentData
{
    public float4 Wander;
    public float4 Eat;
    public float4 Mate;
    public float4 Flee;
}
