using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// Sistema simplificado de movimiento y comportamiento para herbívoros basado en TemplateAgent.
[UpdateAfter(typeof(ObstacleRegistrySystem))]
public partial struct HerbivoreTemplateSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<GridManager>(out var grid) ||
            !SystemAPI.TryGetSingleton<HerbivoreManager>(out var hManager))
            return;

        var obstacles = ObstacleRegistrySystem.Obstacles;
        var rand = Unity.Mathematics.Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000 + 7));
        float2 half = grid.AreaSize * 0.5f;
        int2 bounds = (int2)half;
        float dt = SystemAPI.Time.DeltaTime;

        // Contamos la población actual para limitar la reproducción.
        var herbQuery = SystemAPI.QueryBuilder().WithAll<Herbivore>().Build();
        int population = herbQuery.CalculateEntityCount();

        // Mapas auxiliares para plantas y herbívoros.
        var plantQuery = SystemAPI.QueryBuilder().WithAll<Plant, GridPosition>().Build();
        int plantCount = plantQuery.CalculateEntityCount();
        var plantMap = new NativeParallelHashMap<int2, Entity>(plantCount, Allocator.Temp);
        foreach (var (gp, entity) in SystemAPI.Query<RefRO<GridPosition>>().WithAll<Plant>().WithEntityAccess())
        {
            plantMap.TryAdd(gp.ValueRO.Cell, entity);
        }

        var herbMap = new NativeParallelHashMap<int2, Entity>(population, Allocator.Temp);
        foreach (var (gp, entity) in SystemAPI.Query<RefRO<GridPosition>>().WithAll<Herbivore>().WithEntityAccess())
        {
            herbMap.TryAdd(gp.ValueRO.Cell, entity);
        }

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (transform, herb, gp, energy, repro, health, info, entity) in SystemAPI
                 .Query<RefRW<LocalTransform>, RefRW<Herbivore>, RefRW<GridPosition>, RefRW<Energy>, RefRW<Reproduction>, RefRW<Health>, RefRW<HerbivoreInfo>>().WithEntityAccess())
        {
            var path = state.EntityManager.GetBuffer<PathBufferElement>(entity);
            int2 current = gp.ValueRO.Cell;

            if (path.Length == 0)
            {
                path.Add(new PathBufferElement { Cell = current });
                herb.ValueRW.PathIndex = 0;
            }

            // Comer si hay planta en la celda actual.
            if (plantMap.TryGetValue(current, out var plantEntity))
            {
                var plant = state.EntityManager.GetComponentData<Plant>(plantEntity);
                float eat = herb.ValueRO.EatEnergyRate * dt;
                energy.ValueRW.Value = math.min(energy.ValueRO.Max, energy.ValueRO.Value + eat);
                float healthGain = health.ValueRO.Max * herb.ValueRO.HealthRestorePercent * dt;
                health.ValueRW.Value = math.min(health.ValueRO.Max, health.ValueRO.Value + healthGain);
                plant.Stage = PlantStage.Withering;
                plant.BeingEaten = 1;
                plant.Energy -= eat;
                if (plant.Energy <= 0f)
                {
                    ecb.DestroyEntity(plantEntity);
                    plantMap.Remove(current);
                }
                else
                {
                    state.EntityManager.SetComponentData(plantEntity, plant);
                }
                herb.ValueRW.PathIndex = path.Length; // permanecer en la planta
            }

            // Actualizar temporizador de reproducción.
            repro.ValueRW.Timer = math.max(0f, repro.ValueRO.Timer - dt);

            // Buscar pareja si está listo.
            bool readyToMate = energy.ValueRO.Value >= repro.ValueRO.Threshold && repro.ValueRO.Timer <= 0f && population < hManager.MaxPopulation;
            Entity mate = Entity.Null;
            int2 mateCell = current;
            float bestMateDist = float.MaxValue;
            if (readyToMate)
            {
                int radius = (int)math.ceil(repro.ValueRO.SeekRadius);
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        int2 c = current + new int2(x, y);
                        if (herbMap.TryGetValue(c, out var cand) && cand != entity)
                        {
                            var candEnergy = state.EntityManager.GetComponentData<Energy>(cand);
                            var candRepro = state.EntityManager.GetComponentData<Reproduction>(cand);
                            if (candEnergy.Value >= candRepro.Threshold && candRepro.Timer <= 0f)
                            {
                                float dist = math.lengthsq(new float2(x, y));
                                if (dist < bestMateDist)
                                {
                                    bestMateDist = dist;
                                    mate = cand;
                                    mateCell = c;
                                }
                            }
                        }
                    }
                }
            }

            bool reproduced = false;
            if (mate != Entity.Null && bestMateDist <= repro.ValueRO.MatingDistance * repro.ValueRO.MatingDistance && entity.Index < mate.Index)
            {
                var mateInfo = state.EntityManager.GetComponentData<HerbivoreInfo>(mate);
                int offspringCount = rand.NextInt(repro.ValueRO.MinOffspring, repro.ValueRO.MaxOffspring + 1);
                int gen = math.max(info.ValueRO.Generation, mateInfo.Generation) + 1;
                for (int i = 0; i < offspringCount; i++)
                {
                    int2 spawnCell = current;
                    var child = ecb.Instantiate(hManager.Prefab);
                    ecb.SetComponent(child, new LocalTransform
                    {
                        Position = new float3(spawnCell.x, 0f, spawnCell.y),
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });
                    ecb.AddComponent(child, new GridPosition { Cell = spawnCell });
                    ecb.SetComponent(child, hManager.BaseHealth);
                    ecb.SetComponent(child, hManager.BaseEnergy);
                    var childHerb = hManager.BaseHerbivore;
                    childHerb.Target = spawnCell;
                    childHerb.WaitTimer = rand.NextFloat(0f, 1f);
                    childHerb.PathIndex = 0;
                    ecb.SetComponent(child, childHerb);
                    var childRepro = hManager.BaseReproduction;
                    childRepro.Timer = childRepro.Cooldown;
                    ecb.SetComponent(child, childRepro);
                    var childPath = ecb.AddBuffer<PathBufferElement>(child);
                    childPath.Add(new PathBufferElement { Cell = spawnCell });
                    ecb.SetComponent(child, new HerbivoreInfo
                    {
                        Name = HerbivoreNameGenerator.NextName(),
                        Lifetime = 0f,
                        Generation = gen
                    });
                    population++;
                }
                repro.ValueRW.Timer = repro.ValueRO.Cooldown;
                energy.ValueRW.Value *= (1f - repro.ValueRO.EnergyCostPercent);

                var mateRepro = state.EntityManager.GetComponentData<Reproduction>(mate);
                mateRepro.Timer = mateRepro.Cooldown;
                state.EntityManager.SetComponentData(mate, mateRepro);
                var mateEnergy = state.EntityManager.GetComponentData<Energy>(mate);
                mateEnergy.Value *= (1f - repro.ValueRO.EnergyCostPercent);
                state.EntityManager.SetComponentData(mate, mateEnergy);

                reproduced = true;
            }

            // Redirigir si tiene hambre y no se dirige a una planta.
            if (energy.ValueRO.Value < energy.ValueRO.SeekThreshold && !plantMap.ContainsKey(current))
            {
                bool headingToPlant = path.Length > 0 && plantMap.ContainsKey(path[path.Length - 1].Cell);
                if (!headingToPlant &&
                    FindNearestPlant(current, herb.ValueRO.PlantSeekRadius, plantMap, out var bestCell) &&
                    FindPath(current, bestCell, obstacles, bounds, out var newPath))
                {
                    path.Clear();
                    for (int i = 0; i < newPath.Length; i++)
                        path.Add(new PathBufferElement { Cell = newPath[i] });
                    herb.ValueRW.PathIndex = math.min(1, path.Length);
                    herb.ValueRW.Target = bestCell;
                    herb.ValueRW.WaitTimer = 0f;
                    newPath.Dispose();
                }
            }

            // Si terminó la ruta, decidir nuevo objetivo.
            if (herb.ValueRO.PathIndex >= path.Length)
            {
                herb.ValueRW.WaitTimer -= dt;
                if (herb.ValueRO.WaitTimer > 0f)
                    continue;

                int2 newTarget = current;
                bool seekFood = energy.ValueRO.Value < energy.ValueRO.SeekThreshold;

                if (mate != Entity.Null && !reproduced)
                {
                    newTarget = mateCell;
                }
                else if (seekFood)
                {
                    if (FindNearestPlant(current, herb.ValueRO.PlantSeekRadius, plantMap, out var bestCell))
                        newTarget = bestCell;
                    else
                        newTarget = new int2(rand.NextInt(-bounds.x, bounds.x + 1), rand.NextInt(-bounds.y, bounds.y + 1));
                }
                else
                {
                    newTarget = new int2(rand.NextInt(-bounds.x, bounds.x + 1), rand.NextInt(-bounds.y, bounds.y + 1));
                }

                herb.ValueRW.Target = newTarget;
                herb.ValueRW.WaitTimer = (seekFood || (mate != Entity.Null && !reproduced)) ? 0f : rand.NextFloat(0.5f, 1.5f);

                if (FindPath(current, newTarget, obstacles, bounds, out var newPath))
                {
                    path.Clear();
                    for (int i = 0; i < newPath.Length; i++)
                        path.Add(new PathBufferElement { Cell = newPath[i] });
                    herb.ValueRW.PathIndex = math.min(1, path.Length);
                    newPath.Dispose();
                }
                else
                {
                    herb.ValueRW.PathIndex = path.Length;
                }
            }

            // Movimiento suave a lo largo del camino.
            bool isMoving = false;
            if (herb.ValueRO.PathIndex < path.Length)
            {
                int2 next = path[herb.ValueRO.PathIndex].Cell;
                float3 world = new float3(next.x, 0f, next.y);
                float3 pos = transform.ValueRO.Position;
                float step = herb.ValueRO.MoveSpeed * dt;
                float3 delta = world - pos;
                float dist = math.length(delta);

                if (dist > 0f)
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(delta / dist, math.up());

                if (dist <= step)
                {
                    transform.ValueRW.Position = world;
                    gp.ValueRW.Cell = next;
                    herb.ValueRW.PathIndex++;
                    if (dist > 0f)
                        isMoving = true;
                }
                else
                {
                    transform.ValueRW.Position = pos + delta / dist * step;
                    isMoving = true;
                }
            }

            // Consumo de energía y muerte por inanición.
            float energyCost = herb.ValueRO.IdleEnergyCost * dt;
            if (isMoving)
                energyCost += herb.ValueRO.MoveEnergyCost * herb.ValueRO.MoveSpeed * dt;
            energy.ValueRW.Value = math.max(0f, energy.ValueRO.Value - energyCost);

            if (energy.ValueRO.Value <= energy.ValueRO.DeathThreshold)
            {
                health.ValueRW.Value -= dt;
                if (health.ValueRO.Value <= 0f)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }
            }

            info.ValueRW.Lifetime += dt;
        }

        ecb.Playback(state.EntityManager);
        plantMap.Dispose();
        herbMap.Dispose();
    }

    private static bool FindNearestPlant(int2 current, float radius, NativeParallelHashMap<int2, Entity> plantMap, out int2 bestCell)
    {
        int intRadius = (int)math.ceil(radius);
        float bestDist = float.MaxValue;
        bestCell = current;
        bool found = false;
        float radiusSq = radius * radius;
        for (int x = -intRadius; x <= intRadius; x++)
        {
            for (int y = -intRadius; y <= intRadius; y++)
            {
                int2 c = current + new int2(x, y);
                if (plantMap.ContainsKey(c))
                {
                    float dist = x * x + y * y;
                    if (dist < bestDist && dist <= radiusSq)
                    {
                        bestDist = dist;
                        bestCell = c;
                        found = true;
                    }
                }
            }
        }
        return found;
    }

    private static bool FindPath(int2 start, int2 target, NativeParallelHashSet<int2> obstacles, int2 bounds, out NativeList<int2> path)
    {
        var capacity = (bounds.x * 2 + 1) * (bounds.y * 2 + 1);
        var cameFrom = new NativeHashMap<int2, int2>(capacity, Allocator.Temp);
        var frontier = new NativeQueue<int2>(Allocator.Temp);
        path = new NativeList<int2>(Allocator.Temp);

        frontier.Enqueue(start);
        cameFrom.TryAdd(start, start);

        int2[] dirs = new int2[8]
        {
            new int2(1,0), new int2(-1,0), new int2(0,1), new int2(0,-1),
            new int2(1,1), new int2(1,-1), new int2(-1,1), new int2(-1,-1)
        };

        bool found = false;
        while (frontier.TryDequeue(out var current))
        {
            if (math.all(current == target))
            {
                found = true;
                break;
            }

            for (int i = 0; i < 8; i++)
            {
                int2 dir = dirs[i];
                int2 next = current + dir;
                if (math.abs(next.x) > bounds.x || math.abs(next.y) > bounds.y)
                    continue;
                if (obstacles.Contains(next))
                    continue;
                if (math.abs(dir.x) == 1 && math.abs(dir.y) == 1)
                {
                    int2 sideA = current + new int2(dir.x, 0);
                    int2 sideB = current + new int2(0, dir.y);
                    if (obstacles.Contains(sideA) || obstacles.Contains(sideB))
                        continue;
                }
                if (cameFrom.ContainsKey(next))
                    continue;
                cameFrom.TryAdd(next, current);
                frontier.Enqueue(next);
            }
        }

        if (!found)
        {
            frontier.Dispose();
            cameFrom.Dispose();
            path.Dispose();
            path = default;
            return false;
        }

        int2 p = target;
        while (!math.all(p == start))
        {
            path.Add(p);
            p = cameFrom[p];
        }
        path.Add(start);

        for (int i = 0, j = path.Length - 1; i < j; i++, j--)
        {
            int2 tmp = path[i];
            path[i] = path[j];
            path[j] = tmp;
        }

        frontier.Dispose();
        cameFrom.Dispose();
        return true;
    }
}
