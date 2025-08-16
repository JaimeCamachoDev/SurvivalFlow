using Unity.Entities;
using UnityEngine;


/// Authoring que configura la aparición de herbívoros DOTS.
public class HerbivoreManagerAuthoring : MonoBehaviour
{
    // Prefab del herbívoro que se instanciará.
    public GameObject herbivorePrefab;

    // Prefab de la carne soltada al morir.
    public GameObject meatPrefab;

    // Número de herbívoros iniciales.
    public int initialCount = 20;

    [Header("Genética")]
    // Permite desactivar la herencia de estadísticas.
    public bool enableGenetics = true;

    // Probabilidad de mutación para cada estadística.
    [Range(0f,1f)] public float mutationChance = 0.1f;

    // Escala de variación aplicada al mutar.
    public float mutationScale = 0.1f;

    // Convierte los datos de authoring en un componente de configuración.

    class Baker : Baker<HerbivoreManagerAuthoring>
    {
        public override void Bake(HerbivoreManagerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new HerbivoreManager
            {
                Prefab = GetEntity(authoring.herbivorePrefab, TransformUsageFlags.Dynamic),
                MeatPrefab = GetEntity(authoring.meatPrefab, TransformUsageFlags.Dynamic),
                InitialCount = authoring.initialCount,
                Initialized = 0,
                GeneticsEnabled = (byte)(authoring.enableGenetics ? 1 : 0),
                MutationChance = authoring.mutationChance,
                MutationScale = authoring.mutationScale
            });
        }
    }
}

/// Datos ECS que controlan el spawn de herbívoros.
public struct HerbivoreManager : IComponentData
{
    /// Prefab del herbívoro convertido a entidad.
    public Entity Prefab;

    /// Prefab de la carne generada al morir.
    public Entity MeatPrefab;

    /// Cantidad inicial de herbívoros.
    public int InitialCount;

    /// Bandera para no inicializar dos veces.

    public byte Initialized;

    /// Permite activar o desactivar la genética.
    public byte GeneticsEnabled;

    /// Probabilidad de que una estadística muté en la reproducción.
    public float MutationChance;

    /// Escala de variación aplicada a las mutaciones.
    public float MutationScale;
}
