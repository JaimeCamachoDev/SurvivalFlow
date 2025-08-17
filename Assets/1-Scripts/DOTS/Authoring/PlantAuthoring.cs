using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

using Unity.Transforms;
using UnityEngine;

/// Authoring de un prefab de planta que se convierte a entidad DOTS.
public class PlantAuthoring : MonoBehaviour
{
    // Energía máxima que puede almacenar la planta.
    public float maxEnergy = 100f;

    // Velocidad a la que gana energía por segundo.
    public float energyGainRate = 2f;

    // Porcentaje de energía inicial al nacer.
    [Range(0f, 1f)] public float initialEnergyPercent = 0.05f;

    // Convierte el prefab en entidad DOTS con los componentes necesarios.
    class Baker : Baker<PlantAuthoring>
    {
        public override void Bake(PlantAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Porcentaje mínimo para asegurar visibilidad.
            float initialPercent = math.max(authoring.initialEnergyPercent, 0.2f);

            // Componentes de energía de la planta.
            AddComponent(entity, new Plant
            {
                Energy = authoring.maxEnergy * initialPercent,
                MaxEnergy = authoring.maxEnergy,
                EnergyGainRate = authoring.energyGainRate,
                Stage = PlantStage.Growing,
                BeingEaten = 0
            });

            // Transform inicial según el porcentaje de energía.
            AddComponent(entity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, initialPercent));

            // Color inicial para indicar estado de crecimiento.
            AddComponent(entity, new URPMaterialPropertyBaseColor { Value = new float4(1f, 1f, 0f, 1f) });
        }
    }
}
