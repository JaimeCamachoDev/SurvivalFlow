using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

using Unity.Transforms;
using UnityEngine;

/// Authoring de un prefab de planta que se convierte a entidad DOTS.
public class PlantAuthoring : MonoBehaviour
{
    public float maxGrowth = 100f;
    public float growthRate = 2f;
    [Range(0f, 1f)] public float initialGrowthPercent = 0.05f;

    class Baker : Baker<PlantAuthoring>
    {
        public override void Bake(PlantAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new Plant
            {
                Growth = authoring.maxGrowth * authoring.initialGrowthPercent,
                MaxGrowth = authoring.maxGrowth,
                GrowthRate = authoring.growthRate,
                ScaleStep = 1,
                Stage = PlantStage.Growing
            });

            float initialScale = math.max(authoring.initialGrowthPercent, 0.2f);
            AddComponent(entity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, initialScale));

            // Color inicial para estado de crecimiento
            AddComponent(entity, new URPMaterialPropertyBaseColor { Value = new float4(1f, 1f, 0f, 1f) });
        }
    }
}
