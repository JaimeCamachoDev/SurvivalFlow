using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Genera los herbívoros iniciales configurados por HerbivoreManager.
[BurstCompile]
public partial struct HerbivoreSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Obtenemos el gestor de herbívoros y la cuadrícula.

        if (!SystemAPI.TryGetSingletonRW<HerbivoreManager>(out var managerRw) ||
            !SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;


        // Si ya se inicializó no hacemos nada.
        if (managerRw.ValueRO.Initialized != 0)
            return;

        var manager = managerRw.ValueRO;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var rand = Unity.Mathematics.Random.CreateFromIndex(3);

        // Rango del área jugable en celdas.
        float2 area = grid.AreaSize;
        int2 half = (int2)(area / 2f);

        // Evitamos colocar herbívoros sobre obstáculos existentes.
        var occupied = new NativeParallelHashSet<int2>(manager.InitialCount, Allocator.Temp);
        foreach (var gp in SystemAPI.Query<RefRO<GridPosition>>().WithAll<ObstacleTag>())
            occupied.Add(gp.ValueRO.Cell);

        // Instanciamos el número solicitado de herbívoros en posiciones aleatorias.
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = manager.InitialCount * 20;
        while (spawned < manager.InitialCount && attempts < maxAttempts)
        {
            attempts++;
            int2 cell = new int2(
                rand.NextInt(-half.x, half.x + 1),
                rand.NextInt(-half.y, half.y + 1));

            if (!occupied.Add(cell))
                continue;

            var e = ecb.Instantiate(manager.Prefab);
            ecb.SetComponent(e, new LocalTransform
            {
                Position = new float3(cell.x, 0f, cell.y),
                Rotation = quaternion.identity,
                Scale = 1f
            });
            ecb.AddComponent(e, new GridPosition { Cell = cell });
            ecb.SetComponent(e, new HerbivoreInfo
            {
                Name = HerbivoreNameGenerator.NextName(),
                Lifetime = 0f,
                Generation = 1
            });
            spawned++;
        }

        occupied.Dispose();

        // Marcamos que ya se generaron para no repetir.
        manager.Initialized = 1;
        managerRw.ValueRW = manager;
        ecb.Playback(state.EntityManager);
    }
}
