using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// Autoría que configura la gestión de plantas y el primer conjunto que se generará.
public class PlantManagerAuthoring : MonoBehaviour
{
    // Prefab de planta que se convertirá en entidad.
    public GameObject plantPrefab;

    // Número de plantas iniciales a crear.
    public int initialCount = 100;

    [Header("Patch Settings")]
    // Número de parches de plantas iniciales.
    public int patchCount = 5;

    // Radio de cada parche de plantas.
    public float patchRadius = 5f;

    [Header("Reproduction")]
    // Porcentaje de crecimiento que se consume al reproducirse.
    [Range(0f,1f)]
    public float reproductionCost = 0.2f;
    [Range(1,8)]
    public int reproductionCount = 1;

    // Número de brotes que puede generar cada planta (1-8).
    [Range(1,8)]
    public int reproductionCount = 1;

    // Convierte los datos de authoring a componentes DOTS.
    class Baker : Baker<PlantManagerAuthoring>
    {
        public override void Bake(PlantManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            // Añadimos el componente que almacenará la configuración global de plantas.
            AddComponent(entity, new PlantManager
            {
                Prefab = GetEntity(authoring.plantPrefab, TransformUsageFlags.Dynamic),
                InitialCount = authoring.initialCount,
                PatchCount = authoring.patchCount,
                PatchRadius = authoring.patchRadius,
                ReproductionCost = authoring.reproductionCost,
                ReproductionCount = (byte)math.clamp(authoring.reproductionCount, 1, 8),
                Initialized = 0
            });
        }
    }
}

/// Datos convertidos a ECS para controlar el conjunto de plantas.
public struct PlantManager : IComponentData
{
    /// Prefab convertido a entidad.
    public Entity Prefab;

    /// Cantidad inicial de plantas.
    public int InitialCount;

    /// Número de parches iniciales.
    public int PatchCount;

    /// Radio de cada parche.
    public float PatchRadius;

    /// Coste relativo de crecimiento gastado en reproducción.
    public float ReproductionCost;

    /// Número de brotes por ciclo.
    public byte ReproductionCount;

    /// Bandera interna para evitar inicializar dos veces.
    public byte Initialized;
}
