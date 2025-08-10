using Unity.Entities;
using Unity.Mathematics;

/// <summary>
/// Basic data for a DOTS herbivore.
/// </summary>
public struct Herbivore : IComponentData
{
    public float MoveSpeed;             // Movement speed per second
    public float IdleHungerRate;        // Hunger loss per second when idle
    public float MoveHungerRate;        // Additional hunger loss per speed unit
    public float HungerGain;            // Hunger gained when eating a plant
    public float HealthRestorePercent;  // Percent of max health restored when eating (0-1)
    public float ChangeDirectionInterval; // Seconds between direction changes
    public float DirectionTimer;        // Time until next random direction change
    public float3 MoveDirection;        // Current normalized movement direction
}
