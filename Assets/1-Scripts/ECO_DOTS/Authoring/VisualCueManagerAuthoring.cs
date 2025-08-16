using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// Configura los colores utilizados para representar los estados de las entidades.
public class VisualCueManagerAuthoring : MonoBehaviour
{
    [Header("Plantas")]
    public Color plantGrowingColor = Color.yellow;
    public Color plantMatureColor = Color.green;
    public Color plantWitheringColor = Color.red;

    [Header("Herbívoros")]
    public Color herbivoreNormalColor = Color.green;
    public Color herbivoreHungryColor = Color.yellow;
    public Color herbivoreStarvingColor = Color.red;

    class Baker : Baker<VisualCueManagerAuthoring>
    {
        public override void Bake(VisualCueManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new VisualCueManager
            {
                PlantGrowingColor = new float4(authoring.plantGrowingColor.r, authoring.plantGrowingColor.g, authoring.plantGrowingColor.b, authoring.plantGrowingColor.a),
                PlantMatureColor = new float4(authoring.plantMatureColor.r, authoring.plantMatureColor.g, authoring.plantMatureColor.b, authoring.plantMatureColor.a),
                PlantWitheringColor = new float4(authoring.plantWitheringColor.r, authoring.plantWitheringColor.g, authoring.plantWitheringColor.b, authoring.plantWitheringColor.a),
                HerbivoreNormalColor = new float4(authoring.herbivoreNormalColor.r, authoring.herbivoreNormalColor.g, authoring.herbivoreNormalColor.b, authoring.herbivoreNormalColor.a),
                HerbivoreHungryColor = new float4(authoring.herbivoreHungryColor.r, authoring.herbivoreHungryColor.g, authoring.herbivoreHungryColor.b, authoring.herbivoreHungryColor.a),
                HerbivoreStarvingColor = new float4(authoring.herbivoreStarvingColor.r, authoring.herbivoreStarvingColor.g, authoring.herbivoreStarvingColor.b, authoring.herbivoreStarvingColor.a)
            });
        }
    }
}

/// Datos de colores globales para las visualizaciones.
public struct VisualCueManager : IComponentData
{
    public float4 PlantGrowingColor;
    public float4 PlantMatureColor;
    public float4 PlantWitheringColor;
    public float4 HerbivoreNormalColor;
    public float4 HerbivoreHungryColor;
    public float4 HerbivoreStarvingColor;
}
