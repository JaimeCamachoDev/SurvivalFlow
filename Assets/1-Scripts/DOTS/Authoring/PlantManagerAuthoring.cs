using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// Autoría que configura la gestión de plantas y el primer conjunto que se generará.
public class PlantManagerAuthoring : MonoBehaviour
{
    // Prefab de planta que se convertirá en entidad.
    public GameObject plantPrefab;

    // Número de plantas iniciales a crear.
    public int initialCount = 100;

    [Header("Patch Settings")]
    // Número de parches de plantas iniciales.
    public int patchCount = 5;

    // Radio de cada parche de plantas.
    public float patchRadius = 5f;

    [Header("Plant Settings")]
    // Parámetros básicos de crecimiento de todas las plantas.
    public float maxGrowth = 100f;
    public float growthRate = 2f;
    [Range(0f,1f)] public float initialGrowthPercent = 0.05f;

    [Header("Reproduction")]
    // Intervalo entre intentos de reproducción globales.
    public float reproductionInterval = 10f;

    // Límite máximo de plantas en el mundo.
    public int maxPlants = 200;

    // Distancia mínima y radio de reproducción alrededor del padre (en celdas).
    public float minDistanceBetweenPlants = 1f;
    public float reproductionRadius = 3f;

    // Probabilidad de que aparezca una planta aleatoria cuando hay huecos.
    [Range(0f,1f)] public float randomSpawnChance = 0.1f;

    // Coste relativo de crecimiento gastado al reproducirse.
    [Range(0f,1f)] public float reproductionCost = 0.2f;

    // Número de brotes que puede generar cada intento de reproducción.
    [Range(1,8)] public int reproductionCount = 1;

    // Convierte los datos de authoring a componentes DOTS.
    class Baker : Baker<PlantManagerAuthoring>
    {
        public override void Bake(PlantManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            // Añadimos el componente que almacenará la configuración global de plantas.
            AddComponent(entity, new PlantManager
            {
                Prefab = GetEntity(authoring.plantPrefab, TransformUsageFlags.Dynamic),
                InitialCount = authoring.initialCount,
                PatchCount = authoring.patchCount,
                PatchRadius = authoring.patchRadius,
                PlantMaxGrowth = authoring.maxGrowth,
                PlantGrowthRate = authoring.growthRate,
                InitialGrowthPercent = authoring.initialGrowthPercent,
                ReproductionInterval = authoring.reproductionInterval,
                MaxPlants = authoring.maxPlants,
                MinDistanceBetweenPlants = authoring.minDistanceBetweenPlants,
                ReproductionRadius = authoring.reproductionRadius,
                RandomSpawnChance = authoring.randomSpawnChance,
                ReproductionCost = authoring.reproductionCost,
                ReproductionCount = (byte)math.clamp(authoring.reproductionCount, 1, 8),
                ReproductionTimer = 0f,
                Initialized = 0
            });
        }
    }
}

/// Datos convertidos a ECS para controlar el conjunto de plantas.
public struct PlantManager : IComponentData
{
    /// Prefab convertido a entidad.
    public Entity Prefab;

    /// Cantidad inicial de plantas.
    public int InitialCount;

    /// Número de parches iniciales.
    public int PatchCount;

    /// Radio de cada parche.
    public float PatchRadius;

    /// Parámetros básicos de todas las plantas.
    public float PlantMaxGrowth;
    public float PlantGrowthRate;
    public float InitialGrowthPercent;

    /// Configuración de reproducción global.
    public float ReproductionInterval;
    public int MaxPlants;
    public float MinDistanceBetweenPlants;
    public float ReproductionRadius;
    public float RandomSpawnChance;
    public float ReproductionCost;
    public byte ReproductionCount;

    /// Temporizador interno para controlar el intervalo de reproducción.
    public float ReproductionTimer;

    /// Bandera interna para evitar inicializar dos veces.
    public byte Initialized;
}
