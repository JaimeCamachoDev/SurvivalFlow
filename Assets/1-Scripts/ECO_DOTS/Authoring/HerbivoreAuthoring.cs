using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

/// Authoring para el prefab de herbívoro en DOTS.
public class HerbivoreAuthoring : MonoBehaviour
{
    [Header("Stats")]
    // Vida máxima del herbívoro.
    public float maxHealth = 50f;

    // Energía máxima que puede almacenar.
    public float maxEnergy = 100f;

    // Velocidad de movimiento.
    public float moveSpeed = 2f;

    // Consumo de energía por segundo cuando está quieto.
    public float idleEnergyCost = 1f;

    // Consumo adicional de energía al moverse.
    public float moveEnergyCost = 2f;

    // Tasa de alimentación (energía recuperada por segundo).
    public float eatEnergyRate = 40f;

    // Radio en el que pueden detectar plantas.
    public float plantSeekRadius = 5f;

    [Header("Percepción")]
    public float predatorSenseRadius = 6f;

    [Header("Separación")]
    public float separationRadius = 1.5f;
    public float separationForce = 1f;

    [Header("Decisiones")]
    public float decisionCooldown = 0.2f;

    [Header("Colores de estado")]
    public Color wanderColor = Color.green;
    public Color eatColor = Color.yellow;
    public Color mateColor = Color.cyan;
    public Color fleeColor = Color.red;

    [Header("Reproducción")]
    public float reproductionThreshold = 80f;
    public float reproductionSeekRadius = 6f;
    public float reproductionMatingDistance = 1f;
    public float reproductionCooldown = 10f;
    public int minOffspring = 1;
    public int maxOffspring = 2;
    [Range(0f,1f)] public float reproductionEnergyCostPercent = 0.25f;

    // Porcentaje de vida restaurada al comer.
    [Range(0f,1f)] public float healthRestorePercent = 0.25f;

    // Tiempo entre cambios de dirección aleatorios.
    public float changeDirectionInterval = 2f;

    // Convierte el prefab en una entidad con todos sus componentes.

    class Baker : Baker<HerbivoreAuthoring>
    {
        public override void Bake(HerbivoreAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);


            // Datos de comportamiento del herbívoro.
            AddComponent(entity, new Herbivore
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
                PathIndex = 0
            });

            AddComponent(entity, new HerbivoreState
            {
                Current = HerbivoreBehaviour.Wander,
                DecisionCooldown = authoring.decisionCooldown
            });

            AddComponent(entity, new HerbivoreDecisionTimer { TimeLeft = 0f });

            AddComponent(entity, new PredatorSense { Radius = authoring.predatorSenseRadius });

            AddComponent(entity, new SeparationRadius
            {
                Value = authoring.separationRadius,
                Force = authoring.separationForce
            });

            AddComponent(entity, new StateColor
            {
                Wander = new float4(authoring.wanderColor.r, authoring.wanderColor.g, authoring.wanderColor.b, authoring.wanderColor.a),
                Eat = new float4(authoring.eatColor.r, authoring.eatColor.g, authoring.eatColor.b, authoring.eatColor.a),
                Mate = new float4(authoring.mateColor.r, authoring.mateColor.g, authoring.mateColor.b, authoring.mateColor.a),
                Flee = new float4(authoring.fleeColor.r, authoring.fleeColor.g, authoring.fleeColor.b, authoring.fleeColor.a)
            });

            // Componentes de salud y hambre iniciales.

            AddComponent(entity, new Health
            {
                Value = authoring.maxHealth,
                Max = authoring.maxHealth
            });

            AddComponent(entity, new Energy
            {
                Value = authoring.maxEnergy,
                Max = authoring.maxEnergy,
                SeekThreshold = authoring.maxEnergy * 0.5f,
                DeathThreshold = 0f
            });

            // Reproducción y datos informativos.
            AddComponent(entity, new Reproduction
            {
                Threshold = authoring.reproductionThreshold,
                SeekRadius = authoring.reproductionSeekRadius,
                MatingDistance = authoring.reproductionMatingDistance,
                Cooldown = authoring.reproductionCooldown,
                Timer = 0f,
                MinOffspring = authoring.minOffspring,
                MaxOffspring = authoring.maxOffspring,
                EnergyCostPercent = authoring.reproductionEnergyCostPercent
            });

            AddComponent(entity, new HerbivoreInfo
            {
                Name = new FixedString64Bytes(""),
                Lifetime = 0f,
                Generation = 1
            });

            // Transform y posición inicial del herbívoro.
            AddComponent(entity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            AddComponent(entity, new GridPosition { Cell = int2.zero });

            // Buffer de ruta para que el herbívoro pueda almacenar sus destinos.
            AddBuffer<PathBufferElement>(entity);

            // Etiqueta y color inicial.

            AddComponent<HerbivoreTag>(entity);
            AddComponent(entity, new URPMaterialPropertyBaseColor { Value = new float4(0f, 1f, 0f, 1f) });
        }
    }
}
