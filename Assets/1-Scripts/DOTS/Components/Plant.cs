using Unity.Entities;

/// Estado de crecimiento actual de la planta.
public enum PlantStage : byte
{
    Growing,
    Mature,
    Withering
}

/// Datos de energía de una planta en el sistema DOTS.
public struct Plant : IComponentData
{
    public float Energy;     // Energía actual
    public float MaxEnergy;  // Energía máxima
    public float EnergyGainRate; // Velocidad de obtención de energía

    /// Estado visual/biológico actual.
    public PlantStage Stage;

    /// Marcador temporal cuando un herbívoro la está consumiendo en este frame.
    public byte BeingEaten;
}
