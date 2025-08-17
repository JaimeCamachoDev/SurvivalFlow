using Unity.Entities;

/// <summary>
/// Parámetros de reproducción del herbívoro.
/// </summary>
public struct Reproduction : IComponentData
{
    public float Fertility;     // predisposición [0..1]
    public float MateCooldown;  // tiempo restante
    public float MateCost;      // coste de energía/hambre
    public float MinHealth;     // mínimo requerido
    public float MinEnergy;     // mínimo requerido
}
