using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

using Unity.Transforms;
using UnityEngine;

/// Authoring de un prefab de planta que se convierte a entidad DOTS.
public class PlantAuthoring : MonoBehaviour
{
    // Tamaño máximo que alcanzará la planta.
    public float maxGrowth = 100f;

    // Velocidad de crecimiento por segundo.
    public float growthRate = 2f;

    // Porcentaje del tamaño máximo con el que aparece.
    [Range(0f, 1f)] public float initialGrowthPercent = 0.05f;

    // Convierte el prefab en entidad DOTS con los componentes necesarios.
    class Baker : Baker<PlantAuthoring>
    {
        public override void Bake(PlantAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Componentes de crecimiento de la planta.
            AddComponent(entity, new Plant
            {
                Growth = authoring.maxGrowth * authoring.initialGrowthPercent,
                MaxGrowth = authoring.maxGrowth,
                GrowthRate = authoring.growthRate,
                ScaleStep = 1,
                Stage = PlantStage.Growing,
                BeingEaten = 0
            });

            // Transform inicial según el porcentaje de crecimiento.
            float initialScale = math.max(authoring.initialGrowthPercent, 0.2f);
            AddComponent(entity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, initialScale));

            // Color inicial para indicar estado de crecimiento.
            AddComponent(entity, new URPMaterialPropertyBaseColor { Value = new float4(1f, 1f, 0f, 1f) });
        }
    }
}
