using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Información descriptiva y de telemetría de un herbívoro.
/// </summary>
public struct HerbivoreInfo : IComponentData
{
    /// Nombre único del individuo.
    public FixedString64Bytes Name;
    /// Tiempo de vida en segundos.
    public float Lifetime;
    /// Generación a la que pertenece.
    public int Generation;
}
