using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/// Construye un registro de celdas ocupadas por obstáculos estáticos para que
/// otros sistemas (como los agentes de depuración) puedan consultarlo rápidamente.
[UpdateAfter(typeof(ObstacleSpawnerSystem))]
public partial struct ObstacleRegistrySystem : ISystem
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

    // Job que recorre todas las entidades con GridPosition y ObstacleTag
    // para añadir su celda al conjunto.
    private partial struct BuildObstacleJob : IJobEntity
    {
        public NativeParallelHashSet<int2>.ParallelWriter Obstacles;
        void Execute(in GridPosition gp, in ObstacleTag tag)
        {
            Obstacles.Add(gp.Cell);
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        // Solo construir una vez; si ya hay datos en el conjunto se omite.
        if (Obstacles.Count() > 0)
            return;

        // Programar el job para recolectar todos los obstáculos existentes.
        var job = new BuildObstacleJob { Obstacles = Obstacles.AsParallelWriter() }.ScheduleParallel(state.Dependency);
        state.Dependency = job;
        state.Dependency.Complete();
    }
}
