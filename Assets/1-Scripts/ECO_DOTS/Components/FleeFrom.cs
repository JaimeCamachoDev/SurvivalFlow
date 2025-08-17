using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Indica una posición de amenaza de la que huir.
/// </summary>
public struct FleeFrom : IComponentData
{
    public float3 ThreatPos;
}
