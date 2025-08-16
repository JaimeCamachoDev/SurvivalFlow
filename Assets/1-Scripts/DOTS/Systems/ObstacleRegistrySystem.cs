using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/// Construye un registro de celdas ocupadas por obstáculos estáticos.
public partial struct ObstacleRegistrySystem : ISystem
{
    public static NativeParallelHashSet<int2> Obstacles;

    public void OnCreate(ref SystemState state)
    {
        Obstacles = new NativeParallelHashSet<int2>(1024, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        if (Obstacles.IsCreated) Obstacles.Dispose();
    }

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
        if (Obstacles.Count() > 0)
            return;

        var job = new BuildObstacleJob { Obstacles = Obstacles.AsParallelWriter() }.ScheduleParallel(state.Dependency);
        state.Dependency = job;
        state.Dependency.Complete();
    }
}
