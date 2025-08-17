using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Baker que inicializa los nuevos componentes de herbívoro.
/// </summary>
public class HerbivoreAuthoring : MonoBehaviour
{
    [Header("Vida")]
    public float lifeSpan = 600f;
    public float startHealth = 1f;

    [Header("Metabolismo")]
    public float startEnergy = 0.6f;
    public float startStamina = 0.6f;
    public float startHunger = 0.6f;
    public float baseRate = 0.01f;
    public float moveCost = 0.02f;
    public float sprintCost = 0.04f;
    public float healThreshold = 0.8f;
    public float starveThreshold = 0f;

    [Header("Locomoción")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float visionRadius = 6f;

    [Header("Reproducción")]
    [Range(0f,1f)] public float fertility = 0.5f;
    public float mateCooldown = 120f;
    public float mateCost = 0.2f;
    public float minHealth = 0.8f;
    public float minEnergy = 0.6f;

    class Baker : Baker<HerbivoreAuthoring>
    {
        public override void Bake(HerbivoreAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Herbivore
            {
                Generation = 1,
                Age = 0f,
                LifeSpan = authoring.lifeSpan
            });

            AddComponent(entity, new Health { Value = authoring.startHealth });

            AddComponent(entity, new Metabolism
            {
                Energy = authoring.startEnergy,
                Stamina = authoring.startStamina,
                Hunger = authoring.startHunger,
                BaseRate = authoring.baseRate,
                MoveCost = authoring.moveCost,
                SprintCost = authoring.sprintCost,
                HealThreshold = authoring.healThreshold,
                StarveThreshold = authoring.starveThreshold
            });

            AddComponent(entity, new Locomotion
            {
                WalkSpeed = authoring.walkSpeed,
                RunSpeed = authoring.runSpeed,
                VisionRadius = authoring.visionRadius
            });

            AddComponent(entity, new Reproduction
            {
                Fertility = authoring.fertility,
                MateCooldown = authoring.mateCooldown,
                MateCost = authoring.mateCost,
                MinHealth = authoring.minHealth,
                MinEnergy = authoring.minEnergy
            });

            AddComponent<Intent>(entity);
            AddComponent(entity, new TargetPlant { Plant = Entity.Null });
            AddComponent<LocalTransform>(entity, LocalTransform.Identity);
        }
    }
}
