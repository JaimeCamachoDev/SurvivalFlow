using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/// Construye un registro de celdas ocupadas por obstáculos estáticos para que
/// los sistemas de movimiento basados en la plantilla puedan consultarlo rápidamente.
[UpdateAfter(typeof(ObstacleSpawnerSystem))]
public partial struct MovementObstacleRegistrySystem : ISystem
{
    // Conjunto compartido con las celdas bloqueadas.
    public static NativeParallelHashSet<int2> Obstacles;

    public void OnCreate(ref SystemState state)
    {
        // Reservar memoria persistente para almacenar hasta 1024 obstáculos.
        Obstacles = new NativeParallelHashSet<int2>(1024, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        // Liberar la memoria cuando el sistema se destruye.
        if (Obstacles.IsCreated) Obstacles.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        // Solo construir una vez; si ya hay datos en el conjunto se omite.
        if (Obstacles.Count() > 0)
            return;

        // Recorrer todas las entidades con GridPosition y ObstacleTag
        // para añadir su celda al conjunto.
        foreach (var gp in SystemAPI.Query<RefRO<GridPosition>>().WithAll<ObstacleTag>())
        {
            Obstacles.Add(gp.ValueRO.Cell);
        }
    }
}
