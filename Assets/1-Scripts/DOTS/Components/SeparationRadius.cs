using Unity.Entities;

/// <summary>
/// Parámetros para aplicar una fuerza de separación entre herbívoros.
/// </summary>
public struct SeparationRadius : IComponentData
{
    /// Radio máximo en el que se aplica la separación.
    public float Value;
    /// Intensidad de la fuerza de separación.
    public float Force;
}
