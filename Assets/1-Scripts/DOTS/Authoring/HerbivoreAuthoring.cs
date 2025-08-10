using Unity.Entities;
using UnityEngine;

/// <summary>
/// Authoring component to create herbivore entities.
/// </summary>
public class HerbivoreAuthoring : MonoBehaviour
{
    [Header("Hunger")]
    public float MaxHunger = 100f;
    public float HungerRate = 0.5f;
    public float SeekThreshold = 50f;
    public float DeathThreshold = 0f;

    [Header("Health")]
    public float MaxHealth = 100f;

    class Baker : Baker<HerbivoreAuthoring>
    {
        public override void Bake(HerbivoreAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Hunger
            {
                Value = authoring.MaxHunger,
                Max = authoring.MaxHunger,
                DecreaseRate = authoring.HungerRate,
                SeekThreshold = authoring.SeekThreshold,
                DeathThreshold = authoring.DeathThreshold
            });

            AddComponent(entity, new Health
            {
                Value = authoring.MaxHealth,
                Max = authoring.MaxHealth
            });

            AddComponent<HerbivoreTag>(entity);
        }
    }
}

