using Unity.Entities;
using Unity.Mathematics;

/// Componente para agentes de depuración que se mueven aleatoriamente por la cuadrícula.
/// Cada entidad que lo posea representa un "debug agent" capaz de
/// desplazarse entre celdas del grid y dibujar su recorrido.
public struct DebugAgent : IComponentData
{
    /// Celda objetivo actual hacia la que se desplaza el agente.
    public int2 Target;

    /// Velocidad de movimiento en celdas por segundo.
    /// Esta se inicializa desde el manager y puede variarse para todos
    /// los agentes simultáneamente.
    public float MoveSpeed;
}
