using Unity.Entities;

/// Información de energía de una entidad.
public struct Energy : IComponentData
{
    /// Energía actual disponible.
    public float Value;

    /// Energía máxima que puede almacenar.
    public float Max;

    /// Umbral por debajo del cual la entidad buscará comida.
    public float SeekThreshold;

    /// Umbral mínimo; si se alcanza la entidad empieza a perder salud.
    public float DeathThreshold;
}

