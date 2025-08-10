using Unity.Entities;
using UnityEngine;


/// Authoring que configura la aparición de herbívoros DOTS.
public class HerbivoreManagerAuthoring : MonoBehaviour
{
    // Prefab del herbívoro que se instanciará.
    public GameObject herbivorePrefab;

    // Número de herbívoros iniciales.
    public int initialCount = 20;

    // Convierte los datos de authoring en un componente de configuración.

    class Baker : Baker<HerbivoreManagerAuthoring>
    {
        public override void Bake(HerbivoreManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new HerbivoreManager
            {
                Prefab = GetEntity(authoring.herbivorePrefab, TransformUsageFlags.Dynamic),
                InitialCount = authoring.initialCount,
                Initialized = 0
            });
        }
    }
}

/// Datos ECS que controlan el spawn de herbívoros.
public struct HerbivoreManager : IComponentData
{
    /// Prefab del herbívoro convertido a entidad.
    public Entity Prefab;

    /// Cantidad inicial de herbívoros.
    public int InitialCount;

    /// Bandera para no inicializar dos veces.

    public byte Initialized;
}
