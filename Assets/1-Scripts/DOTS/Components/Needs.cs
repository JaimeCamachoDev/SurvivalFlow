using Unity.Entities;

/// Valores de necesidades básicas de una entidad.
public struct Needs : IComponentData
{
    /// Hambre actual.
    public float Hunger;

    /// Sed actual.
    public float Thirst;

    /// Cantidad de sueño pendiente.
    public float Sleep;
}

/// Datos para controlar la actualización periódica de las necesidades.
public struct NeedTick : IComponentData
{
    /// Tiempo restante hasta la siguiente actualización.
    public float TimeUntilNext;

    /// Período entre cada actualización.
    public float Period;
}

