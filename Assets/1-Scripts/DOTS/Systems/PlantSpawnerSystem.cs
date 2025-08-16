using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Genera el conjunto inicial de plantas definido por PlantManager.
/// Se ejecuta después de la creación de obstáculos para evitar solapamientos.
[BurstCompile]
[UpdateAfter(typeof(ObstacleSpawnerSystem))]
public partial struct PlantSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Obtenemos el gestor de plantas y los datos de la cuadrícula.
        if (!SystemAPI.TryGetSingletonRW<PlantManager>(out var managerRw) ||
            !SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;
        // Aseguramos que los obstáculos se hayan generado antes de comenzar.
        if (!SystemAPI.TryGetSingleton<ObstacleManager>(out var obstacleManager) ||
            obstacleManager.Initialized == 0)
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
        prefabPlant.MaxEnergy = manager.PlantMaxEnergy;
        prefabPlant.EnergyGainRate = manager.PlantEnergyGainRate;
        prefabPlant.Energy = manager.PlantMaxEnergy * manager.InitialEnergyPercent;
        prefabPlant.ScaleStep = 1;
        prefabPlant.Stage = PlantStage.Growing;
        float initialScale = math.max(manager.InitialEnergyPercent, 0.2f);

        // Conjunto de celdas ocupadas y centros de cada parche.
        int2 half = (int2)(area / 2f);
        var obstacleQuery = SystemAPI.QueryBuilder().WithAll<GridPosition, ObstacleTag>().Build();
        int obstacleCount = obstacleQuery.CalculateEntityCount();
        var used = new NativeParallelHashSet<int2>(count + obstacleCount, Allocator.Temp);
        // Evitamos colocar plantas donde ya existen obstáculos.
        var obstacleCells = obstacleQuery.ToComponentDataArray<GridPosition>(Allocator.Temp);
        for (int i = 0; i < obstacleCells.Length; i++)
            used.Add(obstacleCells[i].Cell);
        obstacleCells.Dispose();

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
            var spawnPos = new float3(cell.x, 0f, cell.y);
            ecb.SetComponent(e, new LocalTransform
            {
                Position = spawnPos,
                Rotation = quaternion.identity,
                Scale = initialScale
            });
            ecb.SetComponent(e, new LocalToWorld
            {
                Value = float4x4.TRS(spawnPos, quaternion.identity, new float3(initialScale))
            });

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
