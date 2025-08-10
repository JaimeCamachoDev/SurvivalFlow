using Unity.Entities;

/// <summary>
/// Hunger data for an entity.
/// </summary>
public struct Hunger : IComponentData
{
    /// <summary>Current hunger value.</summary>
    public float Value;

    /// <summary>Maximum hunger value.</summary>
    public float Max;

    /// <summary>Rate at which hunger decreases per second.</summary>
    public float DecreaseRate;

    /// <summary>Below this value the entity will start seeking food.</summary>
    public float SeekThreshold;

    /// <summary>If hunger falls to or below this value the entity dies.</summary>
    public float DeathThreshold;
}

