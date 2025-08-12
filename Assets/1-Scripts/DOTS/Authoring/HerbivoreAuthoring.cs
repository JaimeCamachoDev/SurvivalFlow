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

    // Hambre máxima que puede almacenar.
    public float maxHunger = 100f;

    // Velocidad de movimiento.
    public float moveSpeed = 2f;

    // Consumo de hambre por segundo cuando está quieto.
    public float idleHungerRate = 1f;

    // Consumo adicional de hambre al moverse.
    public float moveHungerRate = 2f;

    // Tasa de alimentación (hambre recuperada por segundo).
    public float eatRate = 40f;

    // Radio en el que pueden detectar plantas.
    public float plantSeekRadius = 5f;

    [Header("Reproducción")]
    public float reproductionThreshold = 80f;
    public float reproductionSeekRadius = 6f;
    public float reproductionMatingDistance = 1f;
    public float reproductionCooldown = 10f;
    public int minOffspring = 1;
    public int maxOffspring = 2;

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
                IdleHungerRate = authoring.idleHungerRate,
                MoveHungerRate = authoring.moveHungerRate,
                EatRate = authoring.eatRate,
                PlantSeekRadius = authoring.plantSeekRadius,
                HealthRestorePercent = authoring.healthRestorePercent,
                ChangeDirectionInterval = authoring.changeDirectionInterval,
                DirectionTimer = 0f,
                MoveDirection = float3.zero,
                MoveRemainder = float3.zero,
                KnownPlantCell = int2.zero,
                HasKnownPlant = 0
            });

            // Componentes de salud y hambre iniciales.

            AddComponent(entity, new Health
            {
                Value = authoring.maxHealth,
                Max = authoring.maxHealth
            });

            AddComponent(entity, new Hunger
            {
                Value = authoring.maxHunger,
                Max = authoring.maxHunger,
                DecreaseRate = authoring.idleHungerRate,
                SeekThreshold = authoring.maxHunger * 0.5f,
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
                MaxOffspring = authoring.maxOffspring
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

            // Etiqueta y color inicial.

            AddComponent<HerbivoreTag>(entity);
            AddComponent(entity, new URPMaterialPropertyBaseColor { Value = new float4(0f, 1f, 0f, 1f) });
        }
    }
}
