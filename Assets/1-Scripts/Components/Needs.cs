using Unity.Entities;

/// Valores de necesidades b�sicas.
public struct Needs : IComponentData
{
    public float Hunger;
    public float Thirst;
    public float Sleep;
}

/// Datos para escalonar los �ticks� de actualizaci�n.
public struct NeedTick : IComponentData
{
    public float TimeUntilNext;
    public float Period;
}
