using Unity.Entities;

/// Información de hambre de una entidad.
public struct Hunger : IComponentData
{
    /// Hambre actual.
    public float Value;

    /// Hambre máxima alcanzable.
    public float Max;

    /// Tasa a la que disminuye el hambre cada segundo.
    public float DecreaseRate;

    /// Umbral por debajo del cual la entidad buscará comida.
    public float SeekThreshold;

    /// Umbral mínimo; si se alcanza la entidad muere.
    public float DeathThreshold;
}

