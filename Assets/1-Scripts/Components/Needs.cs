using Unity.Entities;

/// Valores de necesidades básicas.
public struct Needs : IComponentData
{
    public float Hunger;
    public float Thirst;
    public float Sleep;
}

/// Datos para escalonar los “ticks” de actualización.
public struct NeedTick : IComponentData
{
    public float TimeUntilNext;
    public float Period;
}
