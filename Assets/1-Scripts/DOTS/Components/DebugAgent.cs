using Unity.Entities;
using Unity.Mathematics;

/// Componente para agentes de depuración que se mueven aleatoriamente por la cuadrícula.
public struct DebugAgent : IComponentData
{
    /// Celda objetivo actual.
    public int2 Target;

    /// Velocidad de movimiento en celdas por segundo.
    public float MoveSpeed;
}
