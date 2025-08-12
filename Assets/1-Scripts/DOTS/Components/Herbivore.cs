using Unity.Entities;
using Unity.Mathematics;

/// Datos básicos de un herbívoro en DOTS.
public struct Herbivore : IComponentData
{
    /// Velocidad de movimiento por segundo.
    public float MoveSpeed;

    /// Consumo de hambre por segundo cuando está quieto.
    public float IdleHungerRate;

    /// Consumo adicional de hambre por unidad de velocidad.
    public float MoveHungerRate;

    /// Tasa a la que recupera hambre al comer (por segundo).
    public float EatRate;

    /// Radio en celdas en el que puede detectar plantas.
    public float PlantSeekRadius;

    /// Porcentaje de vida máxima que se restaura al comer (0-1).
    public float HealthRestorePercent;

    /// Intervalo en segundos entre cambios de dirección aleatorios.
    public float ChangeDirectionInterval;

    /// Tiempo restante para el próximo cambio de dirección.
    public float DirectionTimer;

    /// Dirección de movimiento normalizada actual.
    public float3 MoveDirection;

    /// Desplazamiento subcelda acumulado para mantener el movimiento alineado a la cuadrícula.
    public float3 MoveRemainder;
}
