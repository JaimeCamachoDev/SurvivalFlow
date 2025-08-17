using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Configuración de percepción de depredadores para un herbívoro.
/// </summary>
public struct PredatorSense : IComponentData
{
    /// Radio dentro del cual se detectan depredadores.
    public float Radius;
}

/// <summary>
/// Datos de huida activa de un herbívoro.
/// </summary>
public struct Fleeing : IComponentData
{
    /// Tiempo restante de huida.
    public float TimeLeft;
    /// Dirección de escape normalizada.
    public float3 Direction;
}
