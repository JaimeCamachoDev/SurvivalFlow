using Unity.Entities;
using Unity.Mathematics;

/// Componente base para agentes de plantilla que se mueven aleatoriamente por la cuadrícula.
/// Cada entidad que lo posea representa un "template agent" capaz de
/// desplazarse entre celdas del grid y dibujar su recorrido.
public struct TemplateAgent : IComponentData
{
    /// Celda objetivo actual hacia la que se desplaza el agente.
    public int2 Target;

    /// Velocidad de movimiento en celdas por segundo.
    /// Esta se inicializa desde el manager y puede variarse para todos
    /// los agentes simultáneamente.
    public float MoveSpeed;

    /// Tiempo de espera antes de elegir un nuevo objetivo.
    public float WaitTimer;

    /// Índice del siguiente nodo dentro del buffer de ruta.
    public int PathIndex;
}
