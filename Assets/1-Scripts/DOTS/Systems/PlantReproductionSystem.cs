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

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var rand = Unity.Mathematics.Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000 + 1));

        // Recolectamos posiciones de todas las plantas y las maduras.
        var positions = new NativeList<float3>(Allocator.Temp);
        var matureEntities = new NativeList<Entity>(Allocator.Temp);
        var maturePositions = new NativeList<float3>(Allocator.Temp);

        foreach (var (plant, transform, entity) in SystemAPI.Query<RefRO<Plant>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            positions.Add(transform.ValueRO.Position);
            if (plant.ValueRO.Stage == PlantStage.Mature)
            {
                matureEntities.Add(entity);
                maturePositions.Add(transform.ValueRO.Position);
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
            float cost = manager.ReproductionCost * parentPlant.MaxGrowth;

            int children = math.min(manager.ReproductionCount, manager.MaxPlants - totalPlants);
            for (int c = 0; c < children && parentPlant.Growth >= cost; c++)
            {
                bool spawned = false;
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    float2 offset2 = rand.NextFloat2Direction() *
                        rand.NextFloat(manager.MinDistanceBetweenPlants, manager.ReproductionRadius);
                    float3 pos = parentPos + new float3(offset2.x, 0f, offset2.y);
                    pos.x = math.clamp(pos.x, -grid.AreaSize.x * 0.5f, grid.AreaSize.x * 0.5f);
                    pos.z = math.clamp(pos.z, -grid.AreaSize.y * 0.5f, grid.AreaSize.y * 0.5f);

                    bool occupied = false;
                    for (int i = 0; i < positions.Length; i++)
                    {
                        if (math.distance(new float2(pos.x, pos.z), new float2(positions[i].x, positions[i].z)) <
                            manager.MinDistanceBetweenPlants)
                        {
                            occupied = true;
                            break;
                        }
                    }

                    if (!occupied)
                    {
                        var child = ecb.Instantiate(manager.Prefab);
                        var template = state.EntityManager.GetComponentData<Plant>(manager.Prefab);
                        template.MaxGrowth = manager.PlantMaxGrowth;
                        template.GrowthRate = manager.PlantGrowthRate;
                        template.Growth = manager.PlantMaxGrowth * manager.InitialGrowthPercent;
                        template.ScaleStep = 1;
                        template.Stage = PlantStage.Growing;
                        float scale = math.max(manager.InitialGrowthPercent, 0.2f);
                        ecb.SetComponent(child, template);
                        ecb.SetComponent(child, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, scale));
                        ecb.SetComponent(child, new LocalToWorld
                        {
                            Value = float4x4.TRS(pos, quaternion.identity, new float3(scale))
                        });

                        positions.Add(pos);
                        parentPlant.Growth -= cost;
                        spawned = true;
                        totalPlants++;
                        break;
                    }
                }

                if (!spawned)
                    break;
            }

            if (parentPlant.Growth < parentPlant.MaxGrowth)
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

                bool occupied = false;
                for (int i = 0; i < positions.Length; i++)
                {
                    if (math.distance(new float2(pos.x, pos.z), new float2(positions[i].x, positions[i].z)) <
                        manager.MinDistanceBetweenPlants)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    var child = ecb.Instantiate(manager.Prefab);
                    var template = state.EntityManager.GetComponentData<Plant>(manager.Prefab);
                    template.MaxGrowth = manager.PlantMaxGrowth;
                    template.GrowthRate = manager.PlantGrowthRate;
                    template.Growth = manager.PlantMaxGrowth * manager.InitialGrowthPercent;
                    template.ScaleStep = 1;
                    template.Stage = PlantStage.Growing;
                    float scale = math.max(manager.InitialGrowthPercent, 0.2f);
                    ecb.SetComponent(child, template);
                    ecb.SetComponent(child, LocalTransform.FromPositionRotationScale(pos, quaternion.identity, scale));
                    ecb.SetComponent(child, new LocalToWorld
                    {
                        Value = float4x4.TRS(pos, quaternion.identity, new float3(scale))
                    });

                    positions.Add(pos);
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
    }
}

