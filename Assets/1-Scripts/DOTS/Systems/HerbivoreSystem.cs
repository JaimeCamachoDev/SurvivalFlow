using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// Gestiona el movimiento, hambre y alimentación de los herbívoros DOTS.
[BurstCompile]
public partial struct HerbivoreSystem : ISystem
{
    private NativeParallelMultiHashMap<int2, Entity> _plants;
    private NativeList<int2> _plantCells;
    private NativeParallelHashSet<int2> _herbCells;
    private NativeParallelHashMap<int2, Entity> _herbMap;
    private NativeParallelHashSet<int2> _obstacles;

    private static readonly int2[] dirs = new int2[8]
    {
        new int2(1,0),  new int2(-1,0),  new int2(0,1),  new int2(0,-1),
        new int2(1,1), new int2(1,-1), new int2(-1,1), new int2(-1,-1)
    };

    private static readonly int2[] dirs4 = new int2[4]
    {
        new int2(1,0), new int2(-1,0), new int2(0,1), new int2(0,-1)
    };

    public void OnCreate(ref SystemState state)
    {
        _plants = new NativeParallelMultiHashMap<int2, Entity>(1024, Allocator.Persistent);
        _plantCells = new NativeList<int2>(Allocator.Persistent);
        _herbCells = new NativeParallelHashSet<int2>(1024, Allocator.Persistent);
        _herbMap = new NativeParallelHashMap<int2, Entity>(1024, Allocator.Persistent);
        _obstacles = new NativeParallelHashSet<int2>(1024, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        if (_plants.IsCreated) _plants.Dispose();
        if (_plantCells.IsCreated) _plantCells.Dispose();
        if (_herbCells.IsCreated) _herbCells.Dispose();
        if (_herbMap.IsCreated) _herbMap.Dispose();
        if (_obstacles.IsCreated) _obstacles.Dispose();
    }

    [BurstCompile]
    private partial struct BuildPlantJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int2, Entity>.ParallelWriter Plants;
        public NativeList<int2>.ParallelWriter PlantCells;
        void Execute(Entity entity, in GridPosition gp, in Plant tag)
        {
            Plants.Add(gp.Cell, entity);
            PlantCells.Add(gp.Cell);
        }
    }

    [BurstCompile]
    private partial struct BuildHerbMapJob : IJobEntity
    {
        public NativeParallelHashSet<int2>.ParallelWriter Cells;
        public NativeParallelHashMap<int2, Entity>.ParallelWriter Map;
        void Execute(Entity entity, in GridPosition gp, in Herbivore tag)
        {
            Cells.Add(gp.Cell);
            Map.TryAdd(gp.Cell, entity);
        }
    }

    [BurstCompile]
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
        // Comprobamos que existan los gestores necesarios para delimitar el movimiento y depurar obstáculos.
        if (!SystemAPI.TryGetSingleton<GridManager>(out var grid) ||
            !SystemAPI.TryGetSingleton<HerbivoreManager>(out var hManager) ||
            !SystemAPI.TryGetSingletonRW<ObstacleManager>(out var obstacleManager))
            return;

        float dt = SystemAPI.Time.DeltaTime;
        var rand = Unity.Mathematics.Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000 + 5));
        float2 half = grid.AreaSize * 0.5f;
        int2 bounds = (int2)half;

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        _plants.Clear();
        _plantCells.Clear();
        _herbCells.Clear();
        _herbMap.Clear();
        _obstacles.Clear();

        var plantJobHandle = new BuildPlantJob
        {
            Plants = _plants.AsParallelWriter(),
            PlantCells = _plantCells.AsParallelWriter()
        }.ScheduleParallel(state.Dependency);

        var herbJobHandle = new BuildHerbMapJob
        {
            Cells = _herbCells.AsParallelWriter(),
            Map = _herbMap.AsParallelWriter()
        }.ScheduleParallel(plantJobHandle);

        var obstacleJobHandle = new BuildObstacleJob
        {
            Obstacles = _obstacles.AsParallelWriter()
        }.ScheduleParallel(herbJobHandle);

        state.Dependency = obstacleJobHandle;
        state.Dependency.Complete();

        // Devuelve la siguiente celda que acerca al objetivo evitando obstáculos y otros
        // herbívoros. Esta versión evita la búsqueda por anchura costosa del antiguo
        // algoritmo y simplemente evalúa las cuatro direcciones principales para
        // escoger el movimiento que más se aproxima al destino.
        bool TryFindNextStep(int2 start, int2 target, out int2 next)
        {
            next = start;
            float bestDist = float.MaxValue;

            for (int i = 0; i < dirs4.Length; i++)
            {
                int2 cand = start + dirs4[i];
                if (math.abs(cand.x) > bounds.x || math.abs(cand.y) > bounds.y)
                    continue;
                if (_obstacles.Contains(cand))
                    continue;
                if (_herbCells.Contains(cand) && !math.all(cand == target))
                    continue;

                float dist = math.lengthsq((float2)(target - cand));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    next = cand;
                }
            }

            return bestDist < float.MaxValue;
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

            if (hasKnownPlant && !_plants.TryGetFirstValue(herb.ValueRO.KnownPlantCell, out _, out _))
            {
                herb.ValueRW.HasKnownPlant = 0;
                hasKnownPlant = false;
            }

            Entity eatingPlant = Entity.Null;
            bool plantHere = _plants.TryGetFirstValue(currentCell, out eatingPlant, out _);

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
                        if (_plants.TryGetFirstValue(herb.ValueRO.KnownPlantCell, out var targetPlant, out _))
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
                        for (int i = 0; i < _plantCells.Length; i++)
                        {
                            float dist = math.lengthsq((float2)(_plantCells[i] - currentCell));
                            if (dist < bestDist && dist <= radiusSq)
                            {
                                bestDist = dist;
                                target = _plantCells[i];
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
                        for (int i = 0; i < _plantCells.Length; i++)
                        {
                            float dist = math.lengthsq((float2)(_plantCells[i] - currentCell));
                            if (dist < bestDist && dist <= radiusSq)
                            {
                                bestDist = dist;
                                target = _plantCells[i];
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
                                if (_herbMap.TryGetValue(c, out var cand) && cand != entity)
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
                        if (_herbMap.TryGetValue(c, out var other) && other != entity)
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
            if (!hasDirection)
            {
                herb.ValueRW.MoveDirection = float3.zero;
            }
            int2 cell = currentCell;
            if (math.all(herb.ValueRO.MoveDirection == float3.zero))
            {
                transform.ValueRW.Position = new float3(cell.x * grid.CellSize, 0f, cell.y * grid.CellSize);
                herb.ValueRW.MoveRemainder = float3.zero;
            }
            else
            {
                float3 move = herb.ValueRO.MoveDirection * speed * dt + herb.ValueRO.MoveRemainder;
                while (math.abs(move.x) >= 1f || math.abs(move.z) >= 1f)
                {
                    int2 step = int2.zero;
                    if (math.abs(move.x) >= 1f) step.x = (int)math.sign(move.x);
                    if (math.abs(move.z) >= 1f) step.y = (int)math.sign(move.z);
                    int2 next = cell + step;
                    if (step.x != 0 && step.y != 0)
                    {
                        int2 cx = cell + new int2(step.x, 0);
                        int2 cy = cell + new int2(0, step.y);
                        if (math.abs(cx.x) > bounds.x || math.abs(cx.y) > bounds.y ||
                            _obstacles.Contains(cx) || _herbCells.Contains(cx) ||
                            math.abs(cy.x) > bounds.x || math.abs(cy.y) > bounds.y ||
                            _obstacles.Contains(cy) || _herbCells.Contains(cy))
                        {
                            move = float3.zero;
                            herb.ValueRW.MoveRemainder = float3.zero;
                            herb.ValueRW.MoveDirection = float3.zero;
                            break;
                        }
                    }

                    if (math.abs(next.x) > bounds.x || math.abs(next.y) > bounds.y ||
                        _obstacles.Contains(next) || _herbCells.Contains(next))
                    {
                        move = float3.zero;
                        herb.ValueRW.MoveRemainder = float3.zero;
                        herb.ValueRW.MoveDirection = float3.zero;
                        break;
                    }

                    _herbCells.Remove(cell);
                    cell = next;
                    _herbCells.Add(cell);
                    move -= new float3(step.x, 0f, step.y);
                }

                if ((cell.x >= bounds.x && move.x > 0f) || (cell.x <= -bounds.x && move.x < 0f))
                {
                    move.x = 0f;
                    herb.ValueRW.MoveDirection.x = 0f;
                }
                if ((cell.y >= bounds.y && move.z > 0f) || (cell.y <= -bounds.y && move.z < 0f))
                {
                    move.z = 0f;
                    herb.ValueRW.MoveDirection.z = 0f;
                }

                gp.ValueRW.Cell = cell;
                herb.ValueRW.MoveRemainder = move;
                float3 worldOffset = new float3(move.x, 0f, move.z) * grid.CellSize;
                transform.ValueRW.Position = new float3(cell.x * grid.CellSize, 0f, cell.y * grid.CellSize) + worldOffset;
            }

            if (_obstacles.Contains(cell))
                obstacleManager.ValueRW.DebugCrossings++;

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
    }
}
