using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Genera el conjunto inicial de plantas definido por PlantManager.
[BurstCompile]
public partial struct PlantSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Obtenemos el gestor de plantas y los datos de la cuadrícula.
        if (!SystemAPI.TryGetSingletonRW<PlantManager>(out var managerRw) ||
            !SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        // Solo ejecutamos una vez al inicio.
        if (managerRw.ValueRO.Initialized != 0)
            return;

        var manager = managerRw.ValueRO;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var prefab = manager.Prefab;
        int count = manager.InitialCount;
        int patchCount = manager.PatchCount;
        float patchRadius = manager.PatchRadius;
        float2 area = grid.AreaSize;

        var rand = Unity.Mathematics.Random.CreateFromIndex(1);
        var prefabPlant = state.EntityManager.GetComponentData<Plant>(prefab);

        // Conjunto de celdas ocupadas y centros de cada parche.
        int2 half = (int2)(area / 2f);
        var used = new NativeParallelHashSet<int2>(count, Allocator.Temp);
        var centers = new NativeArray<float2>(patchCount, Allocator.Temp);
        for (int p = 0; p < patchCount; p++)
        {
            centers[p] = new float2(
                rand.NextFloat(-area.x / 2f, area.x / 2f),
                rand.NextFloat(-area.y / 2f, area.y / 2f));
        }

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = count * 20;

        // Intentamos poblar la cuadrícula con plantas repartidas en parches.
        while (spawned < count && attempts < maxAttempts)
        {
            var center = centers[rand.NextInt(patchCount)];
            float2 offset = rand.NextFloat2Direction() * rand.NextFloat(0f, patchRadius);
            float2 pos = center + offset;
            int2 cell = new int2((int)math.round(pos.x), (int)math.round(pos.y));
            cell = math.clamp(cell, -half, half);
            attempts++;

            if (!used.Add(cell))
                continue; // Ya hay una planta en esa celda

            var e = ecb.Instantiate(prefab);
            ecb.SetComponent(e, new LocalTransform
            {
                Position = new float3(cell.x, 0f, cell.y),
                Rotation = quaternion.identity,
                Scale = 0.2f
            });

            prefabPlant.ScaleStep = 1;
            prefabPlant.Stage = PlantStage.Growing;
            ecb.SetComponent(e, prefabPlant);
            ecb.AddComponent(e, new GridPosition { Cell = cell });
            spawned++;
        }

        centers.Dispose();
        used.Dispose();

        manager.Initialized = 1;
        managerRw.ValueRW = manager;

        // Ejecutamos todas las instancias pendientes.
        ecb.Playback(state.EntityManager);
    }
}
