using Unity.Entities;
using Unity.Mathematics;

/// Datos básicos de un herbívoro en DOTS.
public struct Herbivore : IComponentData
{
    /// Velocidad de movimiento por segundo.
    public float MoveSpeed;

    /// Consumo de energía por segundo cuando está quieto.
    public float IdleEnergyCost;

    /// Consumo adicional de energía por unidad de velocidad.
    public float MoveEnergyCost;

    /// Tasa a la que recupera energía al comer (por segundo).
    public float EatEnergyRate;

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

    /// Celda recordada de la última planta vista.
    public int2 KnownPlantCell;

    /// Indicador de si posee una planta recordada.
    public byte HasKnownPlant;

    /// Indicador de si actualmente está comiendo una planta.
    public byte IsEating;

    /// Celda objetivo actual para navegación.
    public int2 Target;

    /// Tiempo de espera antes de buscar un nuevo objetivo.
    public float WaitTimer;

    /// Índice de la siguiente celda dentro del buffer de ruta.
    public int PathIndex;

    /// Estado interno del generador aleatorio para comportamiento individual.
    public uint RandomState;
}
