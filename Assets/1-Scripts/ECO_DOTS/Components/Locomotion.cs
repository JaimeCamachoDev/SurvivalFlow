using Unity.Entities;

/// <summary>
/// Datos de movimiento y percepción del herbívoro.
/// </summary>
public struct Locomotion : IComponentData
{
    public float WalkSpeed;
    public float RunSpeed;
    public float VisionRadius;
}
