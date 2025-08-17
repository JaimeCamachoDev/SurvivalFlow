using Unity.Entities;

/// Componente que representa la salud de una entidad.
public struct Health : IComponentData
{
    /// Salud actual.
    public float Value;
}

