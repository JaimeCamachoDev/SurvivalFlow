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

        var herbCells = new NativeParallelHashSet<int2>(1024, Allocator.Temp);
        var herbMap = new NativeParallelHashMap<int2, Entity>(1024, Allocator.Temp);
        foreach (var (gp, e) in SystemAPI.Query<RefRO<GridPosition>>().WithAll<Herbivore>().WithEntityAccess())
        {
            herbCells.Add(gp.ValueRO.Cell);
            herbMap.TryAdd(gp.ValueRO.Cell, e);
        }

        int2[] dirs = new int2[8]
        {
            new int2(1,0),  new int2(-1,0),  new int2(0,1),  new int2(0,-1),
            new int2(1,1), new int2(1,-1), new int2(-1,1), new int2(-1,-1)
        };

        foreach (var (transform, hunger, health, herb, gp, repro, info, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<Hunger>, RefRW<Health>, RefRW<Herbivore>, RefRW<GridPosition>, RefRW<Reproduction>, RefRW<HerbivoreInfo>>().WithEntityAccess())
        {
            int2 currentCell = gp.ValueRO.Cell;
            repro.ValueRW.Timer = math.max(0f, repro.ValueRO.Timer - dt);

            bool isHungry = hunger.ValueRO.Value < hunger.ValueRO.Max;
            bool hasKnownPlant = herb.ValueRO.HasKnownPlant != 0;

            if (hasKnownPlant && !plants.TryGetFirstValue(herb.ValueRO.KnownPlantCell, out _, out _))
            {
                herb.ValueRW.HasKnownPlant = 0;
                hasKnownPlant = false;
            }

            bool isEating = false;
            Entity eatingPlant = Entity.Null;
            if (isHungry && plants.TryGetFirstValue(currentCell, out eatingPlant, out _))
                isEating = true;

            float speed = herb.ValueRO.MoveSpeed;
            bool hasDirection = false;

            if (!isEating)
            {
                if (isHungry)
                {
                    if (hasKnownPlant)
                    {
                        if (plants.TryGetFirstValue(herb.ValueRO.KnownPlantCell, out var targetPlant, out _))
                        {
                            eatingPlant = targetPlant;
                            int2 diff = herb.ValueRO.KnownPlantCell - currentCell;
                            if (!math.all(diff == int2.zero))
                            {
                                int2 step = new int2(math.clamp(diff.x, -1, 1), math.clamp(diff.y, -1, 1));
                                herb.ValueRW.MoveDirection = math.normalize(new float3(step.x, 0f, step.y));
                                hasDirection = true;
                            }
                            else
                            {
                                isEating = true;
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
                            int2 diff = target - currentCell;
                            int2 step = new int2(math.clamp(diff.x, -1, 1), math.clamp(diff.y, -1, 1));
                            herb.ValueRW.MoveDirection = math.normalize(new float3(step.x, 0f, step.y));
                            hasDirection = true;
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

                    bool readyToReproduce = hunger.ValueRO.Value >= repro.ValueRO.Threshold && repro.ValueRO.Timer <= 0f;
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
                                    var candHunger = state.EntityManager.GetComponentData<Hunger>(cand);
                                    if (candRepro.Timer <= 0f && candHunger.Value >= candRepro.Threshold)
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
                                }
                                repro.ValueRW.Timer = repro.ValueRO.Cooldown;
                                var mateRepro = state.EntityManager.GetComponentData<Reproduction>(mate);
                                mateRepro.Timer = mateRepro.Cooldown;
                                state.EntityManager.SetComponentData(mate, mateRepro);
                            }
                            else
                            {
                                int2 diff = mateCell - currentCell;
                                int2 step = new int2(math.clamp(diff.x, -1, 1), math.clamp(diff.y, -1, 1));
                                if (step.x != 0 || step.y != 0)
                                {
                                    herb.ValueRW.MoveDirection = math.normalize(new float3(step.x, 0f, step.y));
                                    hasDirection = true;
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

            if (!herbCells.Contains(targetCell) || math.all(targetCell == currentCell))
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

            float hungerRate = herb.ValueRO.MoveDirection.x == 0f && herb.ValueRO.MoveDirection.z == 0f
                ? herb.ValueRO.IdleHungerRate
                : herb.ValueRO.IdleHungerRate + herb.ValueRO.MoveHungerRate * speed;
            hunger.ValueRW.Value = math.max(0f, hunger.ValueRO.Value - hungerRate * dt);

            if (isEating)
            {
                float eat = herb.ValueRO.EatRate * dt;
                hunger.ValueRW.Value = math.min(hunger.ValueRO.Max, hunger.ValueRO.Value + eat);
                float healthGain = health.ValueRO.Max * herb.ValueRO.HealthRestorePercent * dt;
                health.ValueRW.Value = math.min(health.ValueRO.Max, health.ValueRO.Value + healthGain);

                var plant = state.EntityManager.GetComponentData<Plant>(eatingPlant);
                plant.Stage = PlantStage.Withering;
                plant.BeingEaten = 1;
                plant.Growth -= eat;
                if (plant.Growth <= 0f)
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

            if (hunger.ValueRO.Value <= hunger.ValueRO.DeathThreshold)
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
        plants.Dispose();
        plantCells.Dispose();
        herbCells.Dispose();
        herbMap.Dispose();
    }
}
