using Unity.Entities;

/// <summary>
/// Basic health component.
/// </summary>
public struct Health : IComponentData
{
    /// <summary>Current health.</summary>
    public float Value;

    /// <summary>Maximum health.</summary>
    public float Max;
}

