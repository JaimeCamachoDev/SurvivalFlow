using Unity.Entities;

/// <summary>
/// Estado actual del herbívoro y datos para controlar el ritmo de decisiones.
/// </summary>
public struct HerbivoreState : IComponentData
{
    public HerbivoreBehaviour Current;
    public float DecisionCooldown;
}

/// <summary>
/// Temporizador usado para evaluar la próxima decisión del herbívoro.
/// </summary>
public struct HerbivoreDecisionTimer : IComponentData
{
    public float TimeLeft;
}

/// <summary>
/// Posibles estados del herbívoro en la máquina de decisiones.
/// </summary>
public enum HerbivoreBehaviour : byte
{
    Wander,
    Eat,
    Mate,
    Flee
}
