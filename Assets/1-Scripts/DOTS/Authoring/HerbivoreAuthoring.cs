using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

/// Authoring for DOTS herbivore prefab.
public class HerbivoreAuthoring : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 50f;
    public float maxHunger = 100f;
    public float moveSpeed = 2f;
    public float idleHungerRate = 1f;
    public float moveHungerRate = 2f;
    public float hungerGainOnEat = 40f;
    [Range(0f,1f)] public float healthRestorePercent = 0.25f;
    public float changeDirectionInterval = 2f;

    class Baker : Baker<HerbivoreAuthoring>
    {
        public override void Bake(HerbivoreAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Herbivore
            {
                MoveSpeed = authoring.moveSpeed,
                IdleHungerRate = authoring.idleHungerRate,
                MoveHungerRate = authoring.moveHungerRate,
                HungerGain = authoring.hungerGainOnEat,
                HealthRestorePercent = authoring.healthRestorePercent,
                ChangeDirectionInterval = authoring.changeDirectionInterval,
                DirectionTimer = 0f,
                MoveDirection = float3.zero
            });

            AddComponent(entity, new Health
            {
                Value = authoring.maxHealth,
                Max = authoring.maxHealth
            });

            AddComponent(entity, new Hunger
            {
                Value = authoring.maxHunger,
                Max = authoring.maxHunger,
                DecreaseRate = 0f,
                SeekThreshold = 0f,
                DeathThreshold = 0f
            });

            AddComponent(entity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
            AddComponent<HerbivoreTag>(entity);
            AddComponent(entity, new URPMaterialPropertyBaseColor { Value = new float4(0f, 1f, 0f, 1f) });
        }
    }
}
