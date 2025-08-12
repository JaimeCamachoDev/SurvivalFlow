using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Gestiona el movimiento, hambre y alimentación de los herbívoros DOTS.
[BurstCompile]
public partial struct HerbivoreSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Comprobamos que exista una cuadrícula para delimitar el movimiento.
        if (!SystemAPI.TryGetSingleton<GridManager>(out var grid) ||
            !SystemAPI.TryGetSingleton<HerbivoreManager>(out var hManager))
            return;

        float dt = SystemAPI.Time.DeltaTime;
        var rand = Unity.Mathematics.Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000 + 5));
        float2 half = grid.AreaSize * 0.5f;
        int2 bounds = (int2)half;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var plants = new NativeParallelMultiHashMap<int2, Entity>(1024, Allocator.Temp);
        var plantCells = new NativeList<int2>(Allocator.Temp);
        foreach (var (pgp, pEntity) in SystemAPI.Query<RefRO<GridPosition>>().WithAll<Plant>().WithEntityAccess())
        {
            plants.Add(pgp.ValueRO.Cell, pEntity);
            plantCells.Add(pgp.ValueRO.Cell);
        }

        // Celdas ocupadas por herbívoros para evitar superposiciones y mapa para búsquedas.
        var herbCells = new NativeParallelHashSet<int2>(1024, Allocator.Temp);
        var herbMap = new NativeParallelHashMap<int2, Entity>(1024, Allocator.Temp);
        foreach (var (gp, e) in SystemAPI.Query<RefRO<GridPosition>>().WithAll<Herbivore>().WithEntityAccess())
        {
            herbCells.Add(gp.ValueRO.Cell);
            herbMap.TryAdd(gp.ValueRO.Cell, e);
        }

        // Celdas con obstáculos estáticos del escenario.
        var obstacles = new NativeParallelHashSet<int2>(1024, Allocator.Temp);
        foreach (var gp in SystemAPI.Query<RefRO<GridPosition>>().WithAll<ObstacleTag>())
        {
            obstacles.Add(gp.ValueRO.Cell);
        }

        int2[] dirs = new int2[8]
        {
            new int2(1,0),  new int2(-1,0),  new int2(0,1),  new int2(0,-1),
            new int2(1,1), new int2(1,-1), new int2(-1,1), new int2(-1,-1)
        };

        // Búsqueda simple por anchura para encontrar la siguiente celda hacia un objetivo.
        bool TryFindNextStep(int2 start, int2 target, out int2 next)
        {
            var queue = new NativeQueue<int2>(Allocator.Temp);
            var cameFrom = new NativeParallelHashMap<int2, int2>(1024, Allocator.Temp);
            queue.Enqueue(start);
            cameFrom.TryAdd(start, start);
            int2[] dirs4 = new int2[4]
            {
                new int2(1,0), new int2(-1,0), new int2(0,1), new int2(0,-1)
            };

            bool found = false;
            while (queue.TryDequeue(out var cell))
            {
                if (math.all(cell == target))
                {
                    found = true;
                    break;
                }

                for (int i = 0; i < dirs4.Length; i++)
                {
                    int2 nc = cell + dirs4[i];
                    if (math.abs(nc.x) > bounds.x || math.abs(nc.y) > bounds.y)
                        continue;
                    if (obstacles.Contains(nc))
                        continue;
                    if (herbCells.Contains(nc) && !math.all(nc == target))
                        continue;
                    if (!cameFrom.TryAdd(nc, cell))
                        continue;
                    queue.Enqueue(nc);
                }
            }

            next = start;
            if (found)
            {
                var cur = target;
                var prev = cameFrom[cur];
                while (!math.all(prev == start))
                {
                    cur = prev;
                    prev = cameFrom[cur];
                }
                next = cur;
            }

            queue.Dispose();
            cameFrom.Dispose();
            return found;
        }

        // Recorremos cada herbívoro.
        foreach (var (transform, energy, health, herb, gp, repro, info, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<Energy>, RefRW<Health>, RefRW<Herbivore>, RefRW<GridPosition>, RefRW<Reproduction>, RefRW<HerbivoreInfo>>().WithEntityAccess())
        {
            int2 currentCell = gp.ValueRO.Cell;
            repro.ValueRW.Timer = math.max(0f, repro.ValueRO.Timer - dt);

            bool isFleeing = false;
            if (SystemAPI.HasComponent<Fleeing>(entity))
            {
                var flee = SystemAPI.GetComponent<Fleeing>(entity);
                flee.TimeLeft -= dt;
                if (flee.TimeLeft <= 0f)
                {
                    ecb.RemoveComponent<Fleeing>(entity);
                }
                else
                {
                    isFleeing = true;
                    herb.ValueRW.MoveDirection = math.normalize(flee.Direction);
                    SystemAPI.SetComponent(entity, flee);
                }
            }

            bool needsFood = energy.ValueRO.Value < energy.ValueRO.SeekThreshold;
            bool hasKnownPlant = herb.ValueRO.HasKnownPlant != 0;

            if (hasKnownPlant && !plants.TryGetFirstValue(herb.ValueRO.KnownPlantCell, out _, out _))
            {
                herb.ValueRW.HasKnownPlant = 0;
                hasKnownPlant = false;
            }

            Entity eatingPlant = Entity.Null;
            bool plantHere = plants.TryGetFirstValue(currentCell, out eatingPlant, out _);

            bool isEating = herb.ValueRO.IsEating != 0;
            if (isEating)
            {
                if (!plantHere || energy.ValueRO.Value >= energy.ValueRO.Max)
                {
                    isEating = false;
                    herb.ValueRW.IsEating = 0;
                }
            }
            else if (needsFood && plantHere)
            {
                isEating = true;
                herb.ValueRW.IsEating = 1;
            }

            float speed = herb.ValueRO.MoveSpeed * (isFleeing ? 2f : 1f);
            if (energy.ValueRO.Value <= 0f)
            {
                isFleeing = false;
                speed = herb.ValueRO.MoveSpeed * 0.25f;
            }
            bool hasDirection = isFleeing;

            if (!isEating && !isFleeing)
            {
                if (needsFood)
                {
                    if (hasKnownPlant)
                    {
                        if (plants.TryGetFirstValue(herb.ValueRO.KnownPlantCell, out var targetPlant, out _))
                        {
                            eatingPlant = targetPlant;
                            if (!math.all(herb.ValueRO.KnownPlantCell == currentCell))
                            {
                                if (TryFindNextStep(currentCell, herb.ValueRO.KnownPlantCell, out var nextCell))
                                {
                                    int2 step = nextCell - currentCell;
                                    herb.ValueRW.MoveDirection = math.normalize(new float3(step.x, 0f, step.y));
                                    hasDirection = true;
                                }
                            }
                            else
                            {
                                isEating = true;
                                herb.ValueRW.IsEating = 1;
                            }
                        }
                        else
                        {
                            herb.ValueRW.HasKnownPlant = 0;
                            hasKnownPlant = false;
                        }
                    }

                    if (!hasKnownPlant)
                    {
                        float bestDist = float.MaxValue;
                        int2 target = currentCell;
                        float radiusSq = herb.ValueRO.PlantSeekRadius * herb.ValueRO.PlantSeekRadius;
                        for (int i = 0; i < plantCells.Length; i++)
                        {
                            float dist = math.lengthsq((float2)(plantCells[i] - currentCell));
                            if (dist < bestDist && dist <= radiusSq)
                            {
                                bestDist = dist;
                                target = plantCells[i];
                            }
                        }

                        if (bestDist < float.MaxValue)
                        {
                            herb.ValueRW.KnownPlantCell = target;
                            herb.ValueRW.HasKnownPlant = 1;
                            if (TryFindNextStep(currentCell, target, out var nextCell))
                            {
                                int2 step = nextCell - currentCell;
                                herb.ValueRW.MoveDirection = math.normalize(new float3(step.x, 0f, step.y));
                                hasDirection = true;
                            }
                        }
                        else
                        {
                            herb.ValueRW.DirectionTimer -= dt;
                            if (herb.ValueRO.DirectionTimer <= 0f)
                            {
                                int choice = rand.NextInt(8);
                                int2 d = dirs[choice];
                                herb.ValueRW.MoveDirection = math.normalize(new float3(d.x, 0f, d.y));
                                herb.ValueRW.DirectionTimer = herb.ValueRO.ChangeDirectionInterval * 3f;
                            }
                            speed *= 1.75f;
                            hasDirection = true;
                        }
                    }
                }
                else
                {
                    if (herb.ValueRO.HasKnownPlant == 0)
                    {
                        float bestDist = float.MaxValue;
                        int2 target = currentCell;
                        float radiusSq = herb.ValueRO.PlantSeekRadius * herb.ValueRO.PlantSeekRadius;
                        for (int i = 0; i < plantCells.Length; i++)
                        {
                            float dist = math.lengthsq((float2)(plantCells[i] - currentCell));
                            if (dist < bestDist && dist <= radiusSq)
                            {
                                bestDist = dist;
                                target = plantCells[i];
                            }
                        }
                        if (bestDist < float.MaxValue)
                        {
                            herb.ValueRW.KnownPlantCell = target;
                            herb.ValueRW.HasKnownPlant = 1;
                        }
                    }

                    bool readyToReproduce = energy.ValueRO.Value >= repro.ValueRO.Threshold && repro.ValueRO.Timer <= 0f;
                    if (readyToReproduce)
                    {
                        float bestDist = float.MaxValue;
                        int2 mateCell = currentCell;
                        Entity mate = Entity.Null;
                        int radius = (int)math.ceil(repro.ValueRO.SeekRadius);
                        for (int x = -radius; x <= radius; x++)
                        {
                            for (int y = -radius; y <= radius; y++)
                            {
                                int2 c = currentCell + new int2(x, y);
                                if (herbMap.TryGetValue(c, out var cand) && cand != entity)
                                {
                                    var candRepro = state.EntityManager.GetComponentData<Reproduction>(cand);
                                    var candEnergy = state.EntityManager.GetComponentData<Energy>(cand);
                                    if (candRepro.Timer <= 0f && candEnergy.Value >= candRepro.Threshold)
                                    {
                                        float dist = math.lengthsq(new float2(x, y));
                                        if (dist < bestDist)
                                        {
                                            bestDist = dist;
                                            mate = cand;
                                            mateCell = c;
                                        }
                                    }
                                }
                            }
                        }

                        if (mate != Entity.Null)
                        {
                            if (bestDist <= repro.ValueRO.MatingDistance * repro.ValueRO.MatingDistance)
                            {
                                var mateInfo = state.EntityManager.GetComponentData<HerbivoreInfo>(mate);
                                int offspringCount = rand.NextInt(repro.ValueRO.MinOffspring, repro.ValueRO.MaxOffspring + 1);
                                int gen = math.max(info.ValueRO.Generation, mateInfo.Generation) + 1;
                                for (int i = 0; i < offspringCount; i++)
                                {
                                    int2 spawnCell = currentCell;
                                    var child = ecb.Instantiate(hManager.Prefab);
                                    ecb.SetComponent(child, new LocalTransform
                                    {
                                        Position = new float3(spawnCell.x, 0f, spawnCell.y),
                                        Rotation = quaternion.identity,
                                        Scale = 1f
                                    });
                                    ecb.AddComponent(child, new GridPosition { Cell = spawnCell });
                                    ecb.SetComponent(child, new HerbivoreInfo
                                    {
                                        Name = HerbivoreNameGenerator.NextName(),
                                        Lifetime = 0f,
                                        Generation = gen
                                    });

                                    if (hManager.GeneticsEnabled != 0)
                                    {
                                        var mateHerb = state.EntityManager.GetComponentData<Herbivore>(mate);
                                        var childHerb = herb.ValueRO;
                                        childHerb.MoveSpeed = (herb.ValueRO.MoveSpeed + mateHerb.MoveSpeed) * 0.5f;
                                        childHerb.IdleEnergyCost = (herb.ValueRO.IdleEnergyCost + mateHerb.IdleEnergyCost) * 0.5f;
                                        childHerb.MoveEnergyCost = (herb.ValueRO.MoveEnergyCost + mateHerb.MoveEnergyCost) * 0.5f;
                                        childHerb.EatEnergyRate = (herb.ValueRO.EatEnergyRate + mateHerb.EatEnergyRate) * 0.5f;
                                        childHerb.PlantSeekRadius = (herb.ValueRO.PlantSeekRadius + mateHerb.PlantSeekRadius) * 0.5f;
                                        childHerb.ChangeDirectionInterval = (herb.ValueRO.ChangeDirectionInterval + mateHerb.ChangeDirectionInterval) * 0.5f;

                                        if (rand.NextFloat() < hManager.MutationChance)
                                            childHerb.MoveSpeed += rand.NextFloat(-hManager.MutationScale, hManager.MutationScale);
                                        if (rand.NextFloat() < hManager.MutationChance)
                                            childHerb.IdleEnergyCost += rand.NextFloat(-hManager.MutationScale, hManager.MutationScale);
                                        if (rand.NextFloat() < hManager.MutationChance)
                                            childHerb.MoveEnergyCost += rand.NextFloat(-hManager.MutationScale, hManager.MutationScale);
                                        if (rand.NextFloat() < hManager.MutationChance)
                                            childHerb.EatEnergyRate += rand.NextFloat(-hManager.MutationScale, hManager.MutationScale);
                                        if (rand.NextFloat() < hManager.MutationChance)
                                            childHerb.PlantSeekRadius += rand.NextFloat(-hManager.MutationScale, hManager.MutationScale);
                                        if (rand.NextFloat() < hManager.MutationChance)
                                            childHerb.ChangeDirectionInterval += rand.NextFloat(-hManager.MutationScale, hManager.MutationScale);

                                        ecb.SetComponent(child, childHerb);
                                    }
                                }
                                repro.ValueRW.Timer = repro.ValueRO.Cooldown;
                                var mateRepro = state.EntityManager.GetComponentData<Reproduction>(mate);
                                mateRepro.Timer = mateRepro.Cooldown;
                                state.EntityManager.SetComponentData(mate, mateRepro);
                                energy.ValueRW.Value *= (1f - repro.ValueRO.EnergyCostPercent);
                                var mateEnergy = state.EntityManager.GetComponentData<Energy>(mate);
                                mateEnergy.Value *= (1f - repro.ValueRO.EnergyCostPercent);
                                state.EntityManager.SetComponentData(mate, mateEnergy);
                            }
                            else
                            {
                                if (TryFindNextStep(currentCell, mateCell, out var nextCell))
                                {
                                    int2 step = nextCell - currentCell;
                                    if (step.x != 0 || step.y != 0)
                                    {
                                        herb.ValueRW.MoveDirection = math.normalize(new float3(step.x, 0f, step.y));
                                        hasDirection = true;
                                    }
                                }
                            }
                        }

                        if (!hasDirection)
                        {
                            herb.ValueRW.DirectionTimer -= dt;
                            if (herb.ValueRO.DirectionTimer <= 0f)
                            {
                                int choice = rand.NextInt(8);
                                int2 d = dirs[choice];
                                herb.ValueRW.MoveDirection = math.normalize(new float3(d.x, 0f, d.y));
                                herb.ValueRW.DirectionTimer = herb.ValueRO.ChangeDirectionInterval;
                            }
                            speed *= 0.75f;
                            hasDirection = true;
                        }
                    }
                    else
                    {
                        herb.ValueRW.DirectionTimer -= dt;
                        if (herb.ValueRO.DirectionTimer <= 0f)
                        {
                            int choice = rand.NextInt(8);
                            int2 d = dirs[choice];
                            herb.ValueRW.MoveDirection = math.normalize(new float3(d.x, 0f, d.y));
                            herb.ValueRW.DirectionTimer = herb.ValueRO.ChangeDirectionInterval * 1.5f;
                        }
                        speed *= 0.5f;
                    }
                }
            }

            if (SystemAPI.HasComponent<SeparationRadius>(entity))
            {
                var sep = SystemAPI.GetComponent<SeparationRadius>(entity);
                float3 repulse = float3.zero;
                int radius = (int)math.ceil(sep.Value);
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        int2 c = currentCell + new int2(x, y);
                        if (herbMap.TryGetValue(c, out var other) && other != entity)
                        {
                            float2 diff = new float2(currentCell.x - c.x, currentCell.y - c.y);
                            float distSq = math.lengthsq(diff);
                            if (distSq < sep.Value * sep.Value)
                                repulse += new float3(diff.x, 0f, diff.y) / math.max(distSq, 0.01f);
                        }
                    }
                }
                if (!repulse.Equals(float3.zero))
                {
                    herb.ValueRW.MoveDirection = math.normalize(herb.ValueRO.MoveDirection + repulse * sep.Force);
                    hasDirection = true;
                }
            }

            float3 move = herb.ValueRO.MoveDirection * speed * dt + herb.ValueRO.MoveRemainder;
            int2 delta = int2.zero;

            if (math.abs(herb.ValueRO.MoveDirection.x) > 0f && math.abs(herb.ValueRO.MoveDirection.z) > 0f)
            {
                int stepX = (int)math.floor(math.abs(move.x));
                int stepZ = (int)math.floor(math.abs(move.z));
                int steps = math.min(stepX, stepZ);
                if (steps > 0)
                {
                    delta = new int2((int)math.sign(move.x) * steps, (int)math.sign(move.z) * steps);
                    move -= new float3(delta.x, 0f, delta.y);
                }
            }
            else
            {
                int stepX = (int)math.floor(math.abs(move.x));
                int stepZ = (int)math.floor(math.abs(move.z));
                if (stepX != 0 || stepZ != 0)
                {
                    delta = new int2((int)math.sign(move.x) * stepX, (int)math.sign(move.z) * stepZ);
                    move -= new float3(delta.x, 0f, delta.y);
                }
            }

            herb.ValueRW.MoveRemainder = move;
            int2 targetCell = currentCell + delta;
            targetCell.x = math.clamp(targetCell.x, -bounds.x, bounds.x);
            targetCell.y = math.clamp(targetCell.y, -bounds.y, bounds.y);

            if ((!herbCells.Contains(targetCell) && !obstacles.Contains(targetCell)) || math.all(targetCell == currentCell))
            {
                float3 targetPos = new float3(targetCell.x * grid.CellSize, 0f, targetCell.y * grid.CellSize);
                transform.ValueRW.Position = targetPos;
                gp.ValueRW.Cell = targetCell;
                herbCells.Remove(currentCell);
                herbCells.Add(targetCell);
            }
            else
            {
                transform.ValueRW.Position = new float3(currentCell.x * grid.CellSize, 0f, currentCell.y * grid.CellSize);
                herb.ValueRW.MoveRemainder = float3.zero;
            }

            if (!math.all(herb.ValueRO.MoveDirection == float3.zero))
                transform.ValueRW.Rotation = quaternion.LookRotationSafe(herb.ValueRO.MoveDirection, math.up());

            float energyRate = herb.ValueRO.MoveDirection.x == 0f && herb.ValueRO.MoveDirection.z == 0f
                ? herb.ValueRO.IdleEnergyCost
                : herb.ValueRO.IdleEnergyCost + herb.ValueRO.MoveEnergyCost * speed;
            energy.ValueRW.Value = math.max(0f, energy.ValueRO.Value - energyRate * dt);

            if (isEating)
            {
                float eat = herb.ValueRO.EatEnergyRate * dt;
                energy.ValueRW.Value = math.min(energy.ValueRO.Max, energy.ValueRO.Value + eat);
                float healthGain = health.ValueRO.Max * herb.ValueRO.HealthRestorePercent * dt;
                health.ValueRW.Value = math.min(health.ValueRO.Max, health.ValueRO.Value + healthGain);

                var plant = state.EntityManager.GetComponentData<Plant>(eatingPlant);
                plant.Stage = PlantStage.Withering;
                plant.BeingEaten = 1;
                plant.Energy -= eat;
                if (plant.Energy <= 0f)
                {
                    ecb.DestroyEntity(eatingPlant);
                    herb.ValueRW.HasKnownPlant = 0;
                }
                else
                {
                    state.EntityManager.SetComponentData(eatingPlant, plant);
                }

                herb.ValueRW.MoveDirection = float3.zero;
            }

            herb.ValueRW.IsEating = (byte)(isEating ? 1 : 0);

            if (energy.ValueRO.Value <= energy.ValueRO.DeathThreshold)
            {
                health.ValueRW.Value -= dt;
                if (health.ValueRO.Value <= 0f)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }
            }
            // Tiempo de vida del herbívoro.
            info.ValueRW.Lifetime += dt;
        }

        ecb.Playback(state.EntityManager);
        plants.Dispose();
        plantCells.Dispose();
        herbCells.Dispose();
        herbMap.Dispose();
        obstacles.Dispose();
    }
}
