using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Sistema que controla la reproducción global de las plantas.
/// Solo se intenta un nacimiento por intervalo definido en PlantManager.
[BurstCompile]
public partial struct PlantReproductionSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Obtenemos la configuración global y los datos de la cuadrícula.
        if (!SystemAPI.TryGetSingletonRW<PlantManager>(out var managerRw) ||
            !SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        var manager = managerRw.ValueRO;
        manager.ReproductionTimer += SystemAPI.Time.DeltaTime;
        if (manager.ReproductionTimer < manager.ReproductionInterval)
        {
            managerRw.ValueRW = manager;
            return;
        }
        manager.ReproductionTimer = 0f;

        float cellSize = grid.CellSize;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var rand = Unity.Mathematics.Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000 + 1));
        int2 half = (int2)(grid.AreaSize / (2f * cellSize));

        // Recolectamos las celdas ocupadas y las posiciones de las plantas maduras.
        var positions = new NativeList<float3>(Allocator.Temp);
        var matureEntities = new NativeList<Entity>(Allocator.Temp);
        var maturePositions = new NativeList<float3>(Allocator.Temp);
        var occupied = new NativeParallelHashSet<int2>(manager.MaxPlants, Allocator.Temp);

        // Incluimos celdas de obstáculos ya registrados para impedir nacimientos allí.
        if (ObstacleRegistrySystem.Obstacles.IsCreated)
        {
            var obstacleCells = ObstacleRegistrySystem.Obstacles.ToNativeArray(Allocator.Temp);
            for (int i = 0; i < obstacleCells.Length; i++)
                occupied.Add(obstacleCells[i]);
            obstacleCells.Dispose();
        }

        // Como respaldo, también consultamos los obstáculos existentes en la escena.
        foreach (var gp in SystemAPI.Query<RefRO<GridPosition>>().WithAll<ObstacleTag>())
            occupied.Add(gp.ValueRO.Cell);

        foreach (var (plant, transform, gridPos, entity) in SystemAPI
                     .Query<RefRO<Plant>, RefRO<LocalTransform>, RefRO<GridPosition>>()
                     .WithEntityAccess())
        {
            int2 cell = gridPos.ValueRO.Cell;
            float3 snapPos = new float3(cell.x * cellSize, 0f, cell.y * cellSize);
            positions.Add(snapPos);
            occupied.Add(cell);

            if (plant.ValueRO.Stage == PlantStage.Mature)
            {
                matureEntities.Add(entity);
                maturePositions.Add(snapPos);
            }
        }

        int totalPlants = positions.Length;

        // Intento de reproducción alrededor de una planta madura.
        if (totalPlants < manager.MaxPlants && matureEntities.Length > 0)
        {
            int parentIndex = rand.NextInt(matureEntities.Length);
            var parentEntity = matureEntities[parentIndex];
            var parentPos = maturePositions[parentIndex];
            var parentPlant = state.EntityManager.GetComponentData<Plant>(parentEntity);
            float cost = manager.ReproductionCost * parentPlant.MaxEnergy;

            int children = math.min(manager.ReproductionCount, manager.MaxPlants - totalPlants);
            for (int c = 0; c < children && parentPlant.Energy >= cost; c++)
            {
                bool spawned = false;
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    float minDist = manager.MinDistanceBetweenPlants * cellSize;
                    float radius = manager.ReproductionRadius * cellSize;
                    float2 offset2 = rand.NextFloat2Direction() *
                        rand.NextFloat(minDist, radius);
                    float3 pos = parentPos + new float3(offset2.x, 0f, offset2.y);
                    int2 cell = new int2((int)math.round(pos.x / cellSize), (int)math.round(pos.z / cellSize));
                    cell = math.clamp(cell, -half, half);
                    pos = new float3(cell.x * cellSize, 0f, cell.y * cellSize);

                    if (occupied.Contains(cell))
                        continue;

                    bool tooClose = false;
                    for (int i = 0; i < positions.Length; i++)
                    {
                        if (math.distance(new float2(pos.x, pos.z), new float2(positions[i].x, positions[i].z)) <
                            minDist)
                            {
                                tooClose = true;
                                break;
                            }
                    }

                    if (!tooClose)
                    {
                        var child = ecb.Instantiate(manager.Prefab);
                        var template = state.EntityManager.GetComponentData<Plant>(manager.Prefab);
                        template.MaxEnergy = manager.PlantMaxEnergy;
                        template.EnergyGainRate = manager.PlantEnergyGainRate;
                        template.Energy = manager.PlantMaxEnergy * manager.InitialEnergyPercent;
                        template.ScaleStep = 1;
                        template.Stage = PlantStage.Growing;
                        float scale = math.max(manager.InitialEnergyPercent, 0.2f);
                        ecb.SetComponent(child, template);
                        ecb.SetComponent(child, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, scale));
                        ecb.SetComponent(child, new LocalToWorld
                        {
                            Value = float4x4.TRS(pos, quaternion.identity, new float3(scale))
                        });
                        ecb.AddComponent(child, new GridPosition { Cell = cell });

                        positions.Add(pos);
                        occupied.Add(cell);
                        parentPlant.Energy -= cost;
                        spawned = true;
                        totalPlants++;
                        break;
                    }
                }

                if (!spawned)
                    break;
            }

            if (parentPlant.Energy < parentPlant.MaxEnergy)
                parentPlant.Stage = PlantStage.Growing;
            state.EntityManager.SetComponentData(parentEntity, parentPlant);
        }

        // Siembra aleatoria para repoblar huecos.
        if (totalPlants < manager.MaxPlants &&
            (totalPlants == 0 || rand.NextFloat() < manager.RandomSpawnChance))
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float3 pos = new float3(
                    rand.NextFloat(-grid.AreaSize.x * 0.5f, grid.AreaSize.x * 0.5f),
                    0f,
                    rand.NextFloat(-grid.AreaSize.y * 0.5f, grid.AreaSize.y * 0.5f));
                int2 cell = new int2((int)math.round(pos.x / cellSize), (int)math.round(pos.z / cellSize));
                cell = math.clamp(cell, -half, half);
                pos = new float3(cell.x * cellSize, 0f, cell.y * cellSize);

                if (occupied.Contains(cell))
                    continue;

                bool tooClose = false;
                float minDist = manager.MinDistanceBetweenPlants * cellSize;
                for (int i = 0; i < positions.Length; i++)
                {
                    if (math.distance(new float2(pos.x, pos.z), new float2(positions[i].x, positions[i].z)) <
                        minDist)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    var child = ecb.Instantiate(manager.Prefab);
                    var template = state.EntityManager.GetComponentData<Plant>(manager.Prefab);
                    template.MaxEnergy = manager.PlantMaxEnergy;
                    template.EnergyGainRate = manager.PlantEnergyGainRate;
                    template.Energy = manager.PlantMaxEnergy * manager.InitialEnergyPercent;
                    template.ScaleStep = 1;
                    template.Stage = PlantStage.Growing;
                    float scale = math.max(manager.InitialEnergyPercent, 0.2f);
                    ecb.SetComponent(child, template);
                    ecb.SetComponent(child, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, scale));
                    ecb.SetComponent(child, new LocalToWorld
                    {
                        Value = float4x4.TRS(pos, quaternion.identity, new float3(scale))
                    });
                    ecb.AddComponent(child, new GridPosition { Cell = cell });

                    positions.Add(pos);
                    occupied.Add(cell);
                    totalPlants++;
                    break;
                }
            }
        }

        managerRw.ValueRW = manager;
        ecb.Playback(state.EntityManager);

        positions.Dispose();
        matureEntities.Dispose();
        maturePositions.Dispose();
        occupied.Dispose();
    }
}

