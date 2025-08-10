using Unity.Entities;

/// Datos de crecimiento de una planta en el sistema DOTS.
public struct Plant : IComponentData
{
    public float Growth;     // Tamaño actual
    public float MaxGrowth;  // Tamaño máximo
    public float GrowthRate; // Velocidad de crecimiento
}
