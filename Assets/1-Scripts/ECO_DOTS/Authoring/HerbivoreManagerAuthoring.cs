using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


/// Authoring que configura la aparición de herbívoros DOTS.
public class HerbivoreManagerAuthoring : MonoBehaviour
{
    // Prefab del herbívoro que se instanciará.
    public GameObject herbivorePrefab;

    // Prefab de la carne soltada al morir.
    public GameObject meatPrefab;

    // Número de herbívoros iniciales.
    public int initialCount = 20;

    // Límite máximo de población.
    public int maxPopulation = 100;

    [Header("Stats")]
    public float maxHealth = 50f;
    public float maxEnergy = 100f;
    public float moveSpeed = 2f;
    public float idleEnergyCost = 1f;
    public float moveEnergyCost = 2f;
    public float eatEnergyRate = 40f;
    public float plantSeekRadius = 5f;
    [Range(0f,1f)] public float healthRestorePercent = 0.25f;
    public float changeDirectionInterval = 2f;

    [Header("Reproducción")]
    public float reproductionThreshold = 80f;
    public float reproductionSeekRadius = 6f;
    public float reproductionMatingDistance = 1f;
    public float reproductionCooldown = 10f;
    public int minOffspring = 1;
    public int maxOffspring = 2;
    [Range(0f,1f)] public float reproductionEnergyCostPercent = 0.25f;

    [Header("Genética")]
    // Permite desactivar la herencia de estadísticas.
    public bool enableGenetics = true;

    // Probabilidad de mutación para cada estadística.
    [Range(0f,1f)] public float mutationChance = 0.1f;

    // Escala de variación aplicada al mutar.
    public float mutationScale = 0.1f;

    // Convierte los datos de authoring en un componente de configuración.

    class Baker : Baker<HerbivoreManagerAuthoring>
    {
        public override void Bake(HerbivoreManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new HerbivoreManager
            {
                Prefab = GetEntity(authoring.herbivorePrefab, TransformUsageFlags.Dynamic),
                MeatPrefab = GetEntity(authoring.meatPrefab, TransformUsageFlags.Dynamic),
                InitialCount = authoring.initialCount,
                MaxPopulation = authoring.maxPopulation,
                Initialized = 0,
                GeneticsEnabled = (byte)(authoring.enableGenetics ? 1 : 0),
                MutationChance = authoring.mutationChance,
                MutationScale = authoring.mutationScale,
                BaseHerbivore = new Herbivore
                {
                    MoveSpeed = authoring.moveSpeed,
                    IdleEnergyCost = authoring.idleEnergyCost,
                    MoveEnergyCost = authoring.moveEnergyCost,
                    EatEnergyRate = authoring.eatEnergyRate,
                    PlantSeekRadius = authoring.plantSeekRadius,
                    HealthRestorePercent = authoring.healthRestorePercent,
                    ChangeDirectionInterval = authoring.changeDirectionInterval,
                    DirectionTimer = 0f,
                    MoveDirection = float3.zero,
                    MoveRemainder = float3.zero,
                    KnownPlantCell = int2.zero,
                    HasKnownPlant = 0,
                    IsEating = 0,
                    Target = int2.zero,
                    WaitTimer = 0f,
                    PathIndex = 0,
                    RandomState = 1u
                },
                BaseEnergy = new Energy
                {
                    Value = authoring.maxEnergy,
                    Max = authoring.maxEnergy,
                    SeekThreshold = authoring.maxEnergy * 0.5f,
                    DeathThreshold = 0f
                },
                BaseHealth = new Health
                {
                    Value = authoring.maxHealth,
                    Max = authoring.maxHealth
                },
                BaseReproduction = new Reproduction
                {
                    Threshold = authoring.reproductionThreshold,
                    SeekRadius = authoring.reproductionSeekRadius,
                    MatingDistance = authoring.reproductionMatingDistance,
                    Cooldown = authoring.reproductionCooldown,
                    Timer = authoring.reproductionCooldown,
                    MinOffspring = authoring.minOffspring,
                    MaxOffspring = authoring.maxOffspring,
                    EnergyCostPercent = authoring.reproductionEnergyCostPercent
                }
            });
        }
    }
}

/// Datos ECS que controlan el spawn de herbívoros.
public struct HerbivoreManager : IComponentData
{
    /// Prefab del herbívoro convertido a entidad.
    public Entity Prefab;

    /// Prefab de la carne generada al morir.
    public Entity MeatPrefab;

    /// Cantidad inicial de herbívoros.
    public int InitialCount;

    /// Límite máximo de población.
    public int MaxPopulation;

    /// Bandera para no inicializar dos veces.
    public byte Initialized;

    /// Permite activar o desactivar la genética.
    public byte GeneticsEnabled;

    /// Probabilidad de que una estadística muté en la reproducción.
    public float MutationChance;

    /// Escala de variación aplicada a las mutaciones.
    public float MutationScale;

    /// Estadísticas base para los herbívoros.
    public Herbivore BaseHerbivore;
    public Energy BaseEnergy;
    public Health BaseHealth;
    public Reproduction BaseReproduction;
}
