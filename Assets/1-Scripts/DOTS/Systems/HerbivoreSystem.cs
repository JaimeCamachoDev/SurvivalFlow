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
        if (!SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        float dt = SystemAPI.Time.DeltaTime;
        var rand = Unity.Mathematics.Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000 + 5));
        float2 half = grid.AreaSize * 0.5f;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Mapa de plantas por celda para poder consumirlas y lista para búsqueda.
        var plants = new NativeParallelMultiHashMap<int2, Entity>(1024, Allocator.Temp);
        var plantCells = new NativeList<int2>(Allocator.Temp);
        foreach (var (pgp, pEntity) in SystemAPI.Query<RefRO<GridPosition>>().WithAll<Plant>().WithEntityAccess())
        {
            plants.Add(pgp.ValueRO.Cell, pEntity);
            plantCells.Add(pgp.ValueRO.Cell);
        }

        // Direcciones posibles (4 vecinos cardinales).
        int2[] dirs = new int2[4]
        {
            new int2(1,0), new int2(-1,0), new int2(0,1), new int2(0,-1)
        };

        // Recorremos cada herbívoro.
        foreach (var (transform, hunger, health, herb, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<Hunger>, RefRW<Health>, RefRW<Herbivore>>().WithEntityAccess())
        {
            // Determinar si tiene hambre para correr hacia plantas.
            bool isHungry = hunger.ValueRO.Value <= hunger.ValueRO.SeekThreshold;

            // Selección de dirección: si tiene hambre busca la planta más cercana.
            if (isHungry && plantCells.Length > 0)
            {
                int2 currentCell = new int2(
                    (int)math.floor(transform.ValueRO.Position.x / grid.CellSize),
                    (int)math.floor(transform.ValueRO.Position.z / grid.CellSize));
                float bestDist = float.MaxValue;
                int2 target = currentCell;
                for (int i = 0; i < plantCells.Length; i++)
                {
                    float dist = math.lengthsq((float2)(plantCells[i] - currentCell));
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        target = plantCells[i];
                    }
                }
                float3 targetPos = new float3(target.x * grid.CellSize, 0f, target.y * grid.CellSize);
                herb.ValueRW.MoveDirection = math.normalize(targetPos - transform.ValueRO.Position);
            }
            else
            {
                // Contador para cambiar de dirección aleatoriamente.
                herb.ValueRW.DirectionTimer -= dt;
                if (herb.ValueRO.DirectionTimer <= 0f)
                {
                    int choice = rand.NextInt(5); // 0 = quieto
                    if (choice == 0)
                        herb.ValueRW.MoveDirection = float3.zero;
                    else
                    {
                        int2 d = dirs[choice - 1];
                        herb.ValueRW.MoveDirection = math.normalize(new float3(d.x, 0f, d.y));
                    }
                    herb.ValueRW.DirectionTimer = herb.ValueRO.ChangeDirectionInterval;
                }
            }

            // Calculamos el desplazamiento y lo limitamos dentro del área y celdas.
            float speed = isHungry ? herb.ValueRO.MoveSpeed * 2f : herb.ValueRO.MoveSpeed;
            float3 move = herb.ValueRO.MoveDirection * speed * dt;
            float3 pos = transform.ValueRO.Position + move;
            pos.x = math.clamp(pos.x, -half.x, half.x);
            pos.z = math.clamp(pos.z, -half.y, half.y);
            pos.x = math.round(pos.x / grid.CellSize) * grid.CellSize;
            pos.z = math.round(pos.z / grid.CellSize) * grid.CellSize;
            transform.ValueRW.Position = pos;

            // Celda en la que se encuentra actualmente.
            int2 cell = new int2(
                (int)math.floor(transform.ValueRO.Position.x / grid.CellSize),
                (int)math.floor(transform.ValueRO.Position.z / grid.CellSize));

            // Consumo de hambre según si se mueve o no.
            float hungerRate = herb.ValueRO.MoveDirection.x == 0f && herb.ValueRO.MoveDirection.z == 0f
                ? herb.ValueRO.IdleHungerRate
                : herb.ValueRO.IdleHungerRate + herb.ValueRO.MoveHungerRate * speed;
            hunger.ValueRW.Value -= hungerRate * dt;

            // Si el hambre llega a 0 empezamos a perder vida.
            if (hunger.ValueRO.Value <= 0f)
            {
                health.ValueRW.Value -= dt;
                if (health.ValueRO.Value <= 0f)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }
            }

            // Comprobamos si hay una planta en la celda actual.
            if (plants.TryGetFirstValue(cell, out var plantEntity, out _))
            {
                hunger.ValueRW.Value = math.min(hunger.ValueRO.Max, hunger.ValueRO.Value + herb.ValueRO.HungerGain);
                health.ValueRW.Value = math.min(health.ValueRO.Max,
                    health.ValueRO.Value + health.ValueRO.Max * herb.ValueRO.HealthRestorePercent);
                ecb.DestroyEntity(plantEntity); // La planta es consumida
            }
        }
        // Ejecutamos los cambios y liberamos la memoria usada.
        ecb.Playback(state.EntityManager);
        plants.Dispose();
        plantCells.Dispose();
    }
}
