using Unity.Entities;

/// <summary>
/// Datos básicos de un herbívoro. Mantiene generación, edad y vida máxima.
/// </summary>
public struct Herbivore : IComponentData
{
    /// <summary>Generación del agente.</summary>
    public int Generation;

    /// <summary>Edad acumulada en segundos.</summary>
    public float Age;

    /// <summary>Tiempo de vida esperado en segundos.</summary>
    public float LifeSpan;
}
