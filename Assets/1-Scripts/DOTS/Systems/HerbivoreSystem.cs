using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


/// Gestiona el movimiento, hambre y alimentación de los herbívoros DOTS.
/// TODO: Implementar reproducción agregando un componente de reproducción con umbrales
/// y cooldown, buscando parejas cercanas, acercando a ambos hasta una distancia de
/// apareamiento y generando nuevas entidades de cría al cumplirse las condiciones.
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

        // Mapa de plantas por celda para poder consumirlas y lista para búsqueda.
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

        // Direcciones posibles (8 vecinos alrededor de la celda).
        int2[] dirs = new int2[8]
        {
            new int2(1,0),  new int2(-1,0),  new int2(0,1),  new int2(0,-1),
            new int2(1,1), new int2(1,-1), new int2(-1,1), new int2(-1,-1)
        };

        // Recorremos cada herbívoro.
        foreach (var (transform, hunger, health, herb, gp, repro, info, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<Hunger>, RefRW<Health>, RefRW<Herbivore>, RefRW<GridPosition>, RefRW<Reproduction>, RefRW<HerbivoreInfo>>().WithEntityAccess())
        {
            // Celda actual del herbívoro.
            int2 currentCell = gp.ValueRO.Cell;
            repro.ValueRW.Timer = math.max(0f, repro.ValueRO.Timer - dt);

            // Hambre actual: cuando caen por debajo del umbral buscan comida
            // pero continúan comiendo hasta llenarse por completo.
            bool shouldSeekPlant = hunger.ValueRO.Value <= hunger.ValueRO.SeekThreshold;
            bool isHungry = hunger.ValueRO.Value < hunger.ValueRO.Max;

            bool readyToReproduce = !shouldSeekPlant && hunger.ValueRO.Value >= repro.ValueRO.Threshold && repro.ValueRO.Timer <= 0f;

            float speed = herb.ValueRO.MoveSpeed;
            bool hasDirection = false;

            if (shouldSeekPlant)
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
                    int2 diff = target - currentCell;
                    int2 step = new int2(math.clamp(diff.x, -1, 1), math.clamp(diff.y, -1, 1));
                    if (step.x != 0 || step.y != 0)
                    {
                        herb.ValueRW.MoveDirection = math.normalize(new float3(step.x, 0f, step.y));
                        hasDirection = true;
                    }
                }
                if (!hasDirection)
                {
                    // Sin plantas cercanas, se mueve aleatoriamente para buscarlas.
                    herb.ValueRW.DirectionTimer -= dt;
                    if (herb.ValueRO.DirectionTimer <= 0f)
                    {
                        int choice = rand.NextInt(8);
                        int2 d = dirs[choice];
                        herb.ValueRW.MoveDirection = math.normalize(new float3(d.x, 0f, d.y));
                        herb.ValueRW.DirectionTimer = herb.ValueRO.ChangeDirectionInterval * 0.5f;
                    }
                    speed *= 1.5f;
                    hasDirection = true;
                }
            }
            else if (readyToReproduce)
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
                        // Reproducción
                        var mateInfo = state.EntityManager.GetComponentData<HerbivoreInfo>(mate);
                        int offspringCount = rand.NextInt(repro.ValueRO.MinOffspring, repro.ValueRO.MaxOffspring + 1);
                        int gen = math.max(info.ValueRO.Generation, mateInfo.Generation) + 1;
                        for (int i = 0; i < offspringCount; i++)
                        {
                            int2 cell = currentCell;
                            var child = ecb.Instantiate(hManager.Prefab);
                            ecb.SetComponent(child, new LocalTransform
                            {
                                Position = new float3(cell.x, 0f, cell.y),
                                Rotation = quaternion.identity,
                                Scale = 1f
                            });
                            ecb.AddComponent(child, new GridPosition { Cell = cell });
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
            // Movimiento con acumulación subcelda para permanecer en la cuadrícula.
            float3 move = herb.ValueRO.MoveDirection * speed * dt + herb.ValueRO.MoveRemainder;
            int2 delta = int2.zero;

            // Evitamos "saltos" al movernos en diagonal acumulando pasos en ambos ejes
            // y aplicando el mínimo de ellos como desplazamiento simultáneo.
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
            // Orientamos al herbívoro hacia su dirección de movimiento para dar sensación de giro.
            if (!math.all(herb.ValueRO.MoveDirection == float3.zero))
                transform.ValueRW.Rotation = quaternion.LookRotationSafe(herb.ValueRO.MoveDirection, math.up());

            int2 cell = gp.ValueRO.Cell;

            // Consumo de hambre según si se mueve o no.
            float hungerRate = herb.ValueRO.MoveDirection.x == 0f && herb.ValueRO.MoveDirection.z == 0f
                ? herb.ValueRO.IdleHungerRate
                : herb.ValueRO.IdleHungerRate + herb.ValueRO.MoveHungerRate * speed;
            hunger.ValueRW.Value = math.max(0f, hunger.ValueRO.Value - hungerRate * dt);

            // Si el hambre llega al umbral mínimo empezamos a perder vida.
            if (hunger.ValueRO.Value <= hunger.ValueRO.DeathThreshold)
            {
                health.ValueRW.Value -= dt;
                if (health.ValueRO.Value <= 0f)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }
            }

            // Celda frente a la dirección de movimiento, para comer sin superponerse visualmente.
            int2 forwardCell = cell + new int2(
                (int)math.round(herb.ValueRO.MoveDirection.x),
                (int)math.round(herb.ValueRO.MoveDirection.z));

            // Comprobamos si hay una planta en la celda frontal y comemos hasta llenarnos.
            if (isHungry && plants.TryGetFirstValue(forwardCell, out var plantEntity, out _))
            {
                // Restablecemos hambre y vida de forma gradual.
                float eat = herb.ValueRO.EatRate * dt;
                hunger.ValueRW.Value = math.min(hunger.ValueRO.Max, hunger.ValueRO.Value + eat);
                float healthGain = health.ValueRO.Max * herb.ValueRO.HealthRestorePercent * dt;
                health.ValueRW.Value = math.min(health.ValueRO.Max, health.ValueRO.Value + healthGain);

                // Permanecemos en el lugar mientras comemos.
                herb.ValueRW.MoveDirection = float3.zero;

                // Dañamos a la planta y la marcamos como marchitándose.
                var plant = state.EntityManager.GetComponentData<Plant>(plantEntity);
                plant.Stage = PlantStage.Withering;
                plant.BeingEaten = 1;
                plant.Growth -= eat;
                if (plant.Growth <= 0f)
                    ecb.DestroyEntity(plantEntity);
                else
                    state.EntityManager.SetComponentData(plantEntity, plant);
            }

            // Tiempo de vida del herbívoro.
            info.ValueRW.Lifetime += dt;
        }
        // Ejecutamos los cambios y liberamos la memoria usada.
        ecb.Playback(state.EntityManager);
        plants.Dispose();
        plantCells.Dispose();
        herbCells.Dispose();
        herbMap.Dispose();
    }
}
