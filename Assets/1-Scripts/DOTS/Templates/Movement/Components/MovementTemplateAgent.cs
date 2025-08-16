using Unity.Entities;
using Unity.Mathematics;

/// Plantilla de movimiento base para entidades que recorren la cuadrícula.
/// Esta estructura se utilizará como punto de partida para crear seres que
/// se desplacen de forma eficiente y puedan reutilizar el sistema de
/// pathfinding y movimiento.
public struct MovementTemplateAgent : IComponentData
{
    /// Celda objetivo actual hacia la que se desplaza el agente.
    public int2 Target;

    /// Velocidad de movimiento en celdas por segundo.
    /// Esta se inicializa desde el manager y puede variarse para todos
    /// los agentes simultáneamente.
    public float MoveSpeed;
}
