using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// Sistema de comportamiento básico para los herbívoros DOTS.
/// Replica el comportamiento del script clásico pero evitando
/// asignaciones por cuadro y calculando sólo el siguiente paso
/// hacia el objetivo.
/// </summary>
[UpdateAfter(typeof(ObstacleRegistrySystem))]
[BurstCompile]
public partial struct HerbivoreTemplateSystem : ISystem
{
    private NativeParallelHashMap<int2, Entity> _plants;
    private NativeList<int2> _plantCells;
    private NativeParallelHashSet<int2> _herbCells;
    private NativeParallelHashMap<int2, Entity> _herbMap;

    private static readonly int2[] dirs4 = new int2[4]
    {
        new int2(1,0), new int2(-1,0), new int2(0,1), new int2(0,-1)
    };

    private static readonly int2[] dirs8 = new int2[8]
    {
        new int2(1,0), new int2(-1,0), new int2(0,1), new int2(0,-1),
        new int2(1,1), new int2(1,-1), new int2(-1,1), new int2(-1,-1)
    };

    public void OnCreate(ref SystemState state)
    {
        _plants = new NativeParallelHashMap<int2, Entity>(1024, Allocator.Persistent);
        _plantCells = new NativeList<int2>(Allocator.Persistent);
        _herbCells = new NativeParallelHashSet<int2>(1024, Allocator.Persistent);
        _herbMap = new NativeParallelHashMap<int2, Entity>(1024, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        if (_plants.IsCreated) _plants.Dispose();
        if (_plantCells.IsCreated) _plantCells.Dispose();
        if (_herbCells.IsCreated) _herbCells.Dispose();
        if (_herbMap.IsCreated) _herbMap.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<GridManager>(out var grid) ||
            !SystemAPI.TryGetSingleton<HerbivoreManager>(out var hManager))
            return;

        float dt = SystemAPI.Time.DeltaTime;
        float2 half = grid.AreaSize * 0.5f;
        int2 bounds = (int2)half;
        var obstacles = ObstacleRegistrySystem.Obstacles;

        // Construir mapas de plantas y herbívoros para búsquedas rápidas.
        _plants.Clear();
        _plantCells.Clear();
        foreach (var (gp, entity) in SystemAPI.Query<RefRO<GridPosition>>().WithAll<Plant>().WithEntityAccess())
        {
            _plants.TryAdd(gp.ValueRO.Cell, entity);
            _plantCells.Add(gp.ValueRO.Cell);
        }

        _herbCells.Clear();
        _herbMap.Clear();
        foreach (var (gp, entity) in SystemAPI.Query<RefRO<GridPosition>>().WithAll<Herbivore>().WithEntityAccess())
        {
            _herbCells.Add(gp.ValueRO.Cell);
            _herbMap.TryAdd(gp.ValueRO.Cell, entity);
        }

        int population = _herbMap.Count();
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (transform, herb, gp, energy, repro, health, info, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<Herbivore>, RefRW<GridPosition>, RefRW<Energy>, RefRW<Reproduction>, RefRW<Health>, RefRW<HerbivoreInfo>>().WithEntityAccess())
        {
            int2 current = gp.ValueRO.Cell;
            repro.ValueRW.Timer = math.max(0f, repro.ValueRO.Timer - dt);
            var rand = new Unity.Mathematics.Random(herb.ValueRO.RandomState == 0 ? 1u : herb.ValueRO.RandomState);

            // Comer si hay planta en la celda actual.
            if (_plants.TryGetValue(current, out var plantEntity) && energy.ValueRO.Value < energy.ValueRO.Max)
            {
                float eat = herb.ValueRO.EatEnergyRate * dt;
                energy.ValueRW.Value = math.min(energy.ValueRO.Max, energy.ValueRO.Value + eat);
                float heal = health.ValueRO.Max * herb.ValueRO.HealthRestorePercent * dt;
                health.ValueRW.Value = math.min(health.ValueRO.Max, health.ValueRO.Value + heal);

                var plant = state.EntityManager.GetComponentData<Plant>(plantEntity);
                plant.Stage = PlantStage.Withering;
                plant.BeingEaten = 1;
                plant.Energy -= eat;
                if (plant.Energy <= 0f)
                {
                    ecb.DestroyEntity(plantEntity);
                    _plants.Remove(current);
                }
                else
                {
                    state.EntityManager.SetComponentData(plantEntity, plant);
                }

                herb.ValueRW.MoveDirection = float3.zero;
                herb.ValueRW.HasKnownPlant = 0;
                continue;
            }

            bool needsFood = energy.ValueRO.Value < energy.ValueRO.SeekThreshold;
            bool hasDirection = false;

            if (needsFood)
            {
                if (herb.ValueRO.HasKnownPlant != 0 && !_plants.ContainsKey(herb.ValueRO.KnownPlantCell))
                    herb.ValueRW.HasKnownPlant = 0;

                if (herb.ValueRO.HasKnownPlant == 0)
                {
                    float bestDist = float.MaxValue;
                    int2 best = current;
                    float radiusSq = herb.ValueRO.PlantSeekRadius * herb.ValueRO.PlantSeekRadius;
                    for (int i = 0; i < _plantCells.Length; i++)
                    {
                        float dist = math.lengthsq((float2)(_plantCells[i] - current));
                        if (dist < bestDist && dist <= radiusSq)
                        {
                            bestDist = dist;
                            best = _plantCells[i];
                        }
                    }
                    if (bestDist < float.MaxValue)
                    {
                        herb.ValueRW.KnownPlantCell = best;
                        herb.ValueRW.HasKnownPlant = 1;
                    }
                }

                if (herb.ValueRO.HasKnownPlant != 0 && !math.all(herb.ValueRO.KnownPlantCell == current))
                {
                    if (TryFindNextStep(current, herb.ValueRO.KnownPlantCell, bounds, obstacles, out var nextCell))
                    {
                        int2 step = nextCell - current;
                        herb.ValueRW.MoveDirection = math.normalize(new float3(step.x, 0f, step.y));
                        hasDirection = true;
                    }
                }
            }
            else
            {
                bool canReproduce = energy.ValueRO.Value >= repro.ValueRO.Threshold && repro.ValueRO.Timer <= 0f && population < hManager.MaxPopulation;
                if (canReproduce)
                {
                    float bestDist = float.MaxValue;
                    Entity mate = Entity.Null;
                    int2 mateCell = current;
                    int radius = (int)math.ceil(repro.ValueRO.SeekRadius);
                    for (int x = -radius; x <= radius; x++)
                    {
                        for (int y = -radius; y <= radius; y++)
                        {
                            int2 c = current + new int2(x, y);
                            if (_herbMap.TryGetValue(c, out var cand) && cand != entity)
                            {
                                var candEnergy = state.EntityManager.GetComponentData<Energy>(cand);
                                var candRepro = state.EntityManager.GetComponentData<Reproduction>(cand);
                                if (candEnergy.Value >= candRepro.Threshold && candRepro.Timer <= 0f)
                                {
                                    float dist = x * x + y * y;
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
                        if (bestDist <= repro.ValueRO.MatingDistance * repro.ValueRO.MatingDistance && entity.Index < mate.Index)
                        {
                            var mateInfo = state.EntityManager.GetComponentData<HerbivoreInfo>(mate);
                            int offspring = rand.NextInt(repro.ValueRO.MinOffspring, repro.ValueRO.MaxOffspring + 1);
                            int gen = math.max(info.ValueRO.Generation, mateInfo.Generation) + 1;
                            for (int i = 0; i < offspring; i++)
                            {
                                var child = ecb.Instantiate(hManager.Prefab);
                                ecb.SetComponent(child, new LocalTransform
                                {
                                    Position = new float3(current.x, 0f, current.y),
                                    Rotation = quaternion.identity,
                                    Scale = 1f
                                });
                                ecb.AddComponent(child, new GridPosition { Cell = current });
                                ecb.SetComponent(child, hManager.BaseHealth);
                                ecb.SetComponent(child, hManager.BaseEnergy);
                                var childHerb = hManager.BaseHerbivore;
                                childHerb.DirectionTimer = rand.NextFloat(0f, childHerb.ChangeDirectionInterval);
                                childHerb.RandomState = rand.NextUInt();
                                ecb.SetComponent(child, childHerb);
                                var childRepro = hManager.BaseReproduction;
                                childRepro.Timer = childRepro.Cooldown;
                                ecb.SetComponent(child, childRepro);
                                ecb.SetComponent(child, new HerbivoreInfo
                                {
                                    Name = HerbivoreNameGenerator.NextName(),
                                    Lifetime = 0f,
                                    Generation = gen
                                });
                            }

                            repro.ValueRW.Timer = repro.ValueRO.Cooldown;
                            energy.ValueRW.Value *= (1f - repro.ValueRO.EnergyCostPercent);
                            var mateRepro = state.EntityManager.GetComponentData<Reproduction>(mate);
                            mateRepro.Timer = mateRepro.Cooldown;
                            state.EntityManager.SetComponentData(mate, mateRepro);
                            var mateEnergy = state.EntityManager.GetComponentData<Energy>(mate);
                            mateEnergy.Value *= (1f - repro.ValueRO.EnergyCostPercent);
                            state.EntityManager.SetComponentData(mate, mateEnergy);
                            population += offspring;
                        }
                        else if (TryFindNextStep(current, mateCell, bounds, obstacles, out var nextCell))
                        {
                            int2 step = nextCell - current;
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
                    int choice = rand.NextInt(dirs8.Length);
                    int2 d = dirs8[choice];
                    herb.ValueRW.MoveDirection = math.normalize(new float3(d.x, 0f, d.y));
                    herb.ValueRW.DirectionTimer = rand.NextFloat(herb.ValueRO.ChangeDirectionInterval * 0.5f,
                                                                herb.ValueRO.ChangeDirectionInterval * 1.5f);
                }
            }

            // Movimiento en celdas discretas con resto subcelda.
            float3 move = herb.ValueRO.MoveDirection * herb.ValueRO.MoveSpeed * dt + herb.ValueRO.MoveRemainder;
            int2 cell = current;
            while (math.abs(move.x) >= 1f || math.abs(move.z) >= 1f)
            {
                int2 step = int2.zero;
                if (math.abs(move.x) >= 1f) step.x = (int)math.sign(move.x);
                if (math.abs(move.z) >= 1f) step.y = (int)math.sign(move.z);
                int2 next = cell + step;
                if (math.abs(next.x) > bounds.x || math.abs(next.y) > bounds.y ||
                    obstacles.Contains(next) || _herbCells.Contains(next))
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

            cell.x = math.clamp(cell.x, -bounds.x, bounds.x);
            cell.y = math.clamp(cell.y, -bounds.y, bounds.y);
            gp.ValueRW.Cell = cell;
            herb.ValueRW.MoveRemainder = move;
            float3 pos = new float3(cell.x * grid.CellSize, 0f, cell.y * grid.CellSize) +
                         new float3(move.x, 0f, move.z) * grid.CellSize;
            pos.x = math.clamp(pos.x, -bounds.x * grid.CellSize, bounds.x * grid.CellSize);
            pos.z = math.clamp(pos.z, -bounds.y * grid.CellSize, bounds.y * grid.CellSize);
            transform.ValueRW.Position = pos;
            if (!math.all(herb.ValueRO.MoveDirection == float3.zero))
                transform.ValueRW.Rotation = quaternion.LookRotationSafe(herb.ValueRO.MoveDirection, math.up());

            // Consumo de energía y muerte por inanición.
            float cost = herb.ValueRO.IdleEnergyCost;
            if (!math.all(herb.ValueRO.MoveDirection == float3.zero))
                cost += herb.ValueRO.MoveEnergyCost * herb.ValueRO.MoveSpeed;
            energy.ValueRW.Value = math.max(0f, energy.ValueRO.Value - cost * dt);

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
            herb.ValueRW.RandomState = rand.state;
        }

        ecb.Playback(state.EntityManager);
    }

    /// <summary>
    /// Calcula la siguiente celda que acerca al objetivo evitando obstáculos
    /// y otras entidades.
    /// </summary>
    private bool TryFindNextStep(int2 start, int2 target, int2 bounds, NativeParallelHashSet<int2> obstacles, out int2 next)
    {
        next = start;
        float bestDist = float.MaxValue;
        for (int i = 0; i < dirs4.Length; i++)
        {
            int2 cand = start + dirs4[i];
            if (math.abs(cand.x) > bounds.x || math.abs(cand.y) > bounds.y)
                continue;
            if (obstacles.Contains(cand))
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
}

