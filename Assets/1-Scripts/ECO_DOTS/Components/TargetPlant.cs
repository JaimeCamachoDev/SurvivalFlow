using Unity.Entities;

/// <summary>
/// Referencia a una planta objetivo para el herbívoro.
/// </summary>
public struct TargetPlant : IComponentData
{
    public Entity Plant;
}
