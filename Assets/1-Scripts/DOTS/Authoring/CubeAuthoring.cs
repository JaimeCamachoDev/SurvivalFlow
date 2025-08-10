using Unity.Entities;
using UnityEngine;

public class CubeAuthoring : MonoBehaviour
{
    class Baker : Baker<CubeAuthoring>
    {
        public override void Bake(CubeAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Añade aquí los componentes que quieras que tenga el cubo
            // p.ej. Needs, NeedTick, GridPosition, etc.
            // AddComponent(entity, new Needs());
            // AddComponent(entity, new GridPosition());
        }
    }
}
