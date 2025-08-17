using Unity.Entities;

/// <summary>
/// Valores metabólicos normalizados del herbívoro.
/// </summary>
public struct Metabolism : IComponentData
{
    public float Energy;      // [0..1]
    public float Stamina;     // [0..1]
    public float Hunger;      // [0..1] 1=saciado
    public float BaseRate;    // gasto por segundo en reposo
    public float MoveCost;    // gasto adicional por desplazamiento
    public float SprintCost;  // gasto adicional por correr
    public float HealThreshold;   // ≥ regenerar salud
    public float StarveThreshold; // pérdida de salud
}
