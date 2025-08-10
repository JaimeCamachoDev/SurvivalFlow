using Unity.Entities;

/// Estado de crecimiento actual de la planta.
public enum PlantStage : byte
{
    Growing,
    Mature,
    Withering
}

/// Datos de crecimiento de una planta en el sistema DOTS.
public struct Plant : IComponentData
{
    public float Growth;     // Tamaño actual
    public float MaxGrowth;  // Tamaño máximo
    public float GrowthRate; // Velocidad de crecimiento

    /// Último escalón de escala aplicado (1..5) para evitar cambios cada frame.
    public byte ScaleStep;

    /// Estado visual/biológico actual.
    public PlantStage Stage;
}
