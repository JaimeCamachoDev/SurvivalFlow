using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// Sistema de movimiento reutilizable basado en la plantilla de agentes.
[UpdateAfter(typeof(MovementObstacleRegistrySystem))]
public partial struct MovementTemplateSystem : ISystem
{
    private static readonly int2[] Directions = new int2[8]
    {
        new int2(1,0), new int2(-1,0), new int2(0,1), new int2(0,-1),
        new int2(1,1), new int2(1,-1), new int2(-1,1), new int2(-1,-1)
    };
    public void OnUpdate(ref SystemState state)
    {
        // Necesitamos la información de la grilla para limitar el movimiento.
        if (!SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        // Copiar el registro de obstáculos a una variable local para usarlo en las búsquedas.
        var obstacles = MovementObstacleRegistrySystem.Obstacles;

        // Calcular los límites de la grilla (mitad del tamaño del área).
        float2 half = grid.AreaSize * 0.5f;
        int2 bounds = (int2)half;

        // Crear un generador aleatorio que dependa del tiempo para variar los caminos.
        var rand = Unity.Mathematics.Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000 + 13));

        // Iterar sobre todas las entidades basadas en la plantilla de movimiento.
        foreach (var (transform, agent, gp, path) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<MovementTemplateAgent>, RefRW<GridPosition>, DynamicBuffer<MovementPathElement>>())
        {
            // Posición actual del agente en celdas.
            int2 current = gp.ValueRO.Cell;

            // Si ya llegó a su objetivo, seleccionar uno nuevo aleatorio y limpiar camino.
            if (math.all(current == agent.ValueRO.Target))
            {
                int2 newTarget;
                do
                {
                    newTarget = new int2(rand.NextInt(-bounds.x, bounds.x + 1), rand.NextInt(-bounds.y, bounds.y + 1));
                } while (obstacles.Contains(newTarget));
                agent.ValueRW.Target = newTarget;
                path.Clear();
            }

            // Calcular el camino solo si no existe uno almacenado.
            if (path.Length == 0)
            {
                if (!FindPath(current, agent.ValueRO.Target, obstacles, bounds, path))
                {
                    // Si falla, elegir otro objetivo y reintentar.
                    int2 newTarget;
                    do
                    {
                        newTarget = new int2(rand.NextInt(-bounds.x, bounds.x + 1), rand.NextInt(-bounds.y, bounds.y + 1));
                    } while (obstacles.Contains(newTarget));
                    agent.ValueRW.Target = newTarget;
                    path.Clear();
                    if (!FindPath(current, newTarget, obstacles, bounds, path))
                        continue; // No se encontró camino, pasar a la siguiente entidad.
                }
            }

            // Dibujar la ruta calculada en la vista de escena del editor.
            for (int i = 0; i < path.Length - 1; i++)
            {
                float3 a = new float3(path[i].Cell.x, 0f, path[i].Cell.y);
                float3 b = new float3(path[i + 1].Cell.x, 0f, path[i + 1].Cell.y);
                Debug.DrawLine(a, b, Color.cyan);
            }

            // Desplazar suavemente al agente hacia la siguiente celda del camino.
            if (path.Length > 1)
            {
                int2 next = path[1].Cell;
                float3 world = new float3(next.x, 0f, next.y);
                float3 pos = transform.ValueRO.Position;
                float step = agent.ValueRO.MoveSpeed * SystemAPI.Time.DeltaTime;
                float3 delta = world - pos;
                float dist = math.length(delta);

                // Orientar al agente hacia la dirección del movimiento para que avance "de frente".
                if (dist > 0f)
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(delta / dist, math.up());

                if (dist <= step)
                {
                    // Llegó a la siguiente celda.
                    transform.ValueRW.Position = world;
                    gp.ValueRW.Cell = next;
                    path.RemoveAt(0); // Avanzar en el buffer
                }
                else
                {
                    // Avanzar una fracción hacia la celda destino.
                    transform.ValueRW.Position = pos + delta / dist * step;
                }
            }
        }
    }

    private static bool FindPath(int2 start, int2 target, NativeParallelHashSet<int2> obstacles, int2 bounds, DynamicBuffer<MovementPathElement> path)
    {
        // Capacidad máxima estimada del grid para reservar memoria.
        var capacity = (bounds.x * 2 + 1) * (bounds.y * 2 + 1);
        var cameFrom = new NativeHashMap<int2, int2>(capacity, Allocator.Temp);
        var frontier = new NativeQueue<int2>(Allocator.Temp);
        var tmpPath = new NativeList<int2>(Allocator.Temp);

        // Inicializar la búsqueda con la celda de partida.
        frontier.Enqueue(start);
        cameFrom.TryAdd(start, start);

        bool found = false;

        // Búsqueda en anchura clásica (BFS).
        while (frontier.TryDequeue(out var current))
        {
            if (math.all(current == target))
            {
                found = true;
                break; // Se alcanzó el objetivo.
            }

            // Explorar las celdas vecinas.
            for (int i = 0; i < Directions.Length; i++)
            {
                int2 dir = Directions[i];
                int2 next = current + dir;

                // Ignorar si está fuera de los límites.
                if (math.abs(next.x) > bounds.x || math.abs(next.y) > bounds.y)
                    continue;

                // Ignorar celdas ocupadas por obstáculos.
                if (obstacles.Contains(next))
                    continue;

                // Para diagonales, evitar "cortar esquinas".
                if (math.abs(dir.x) == 1 && math.abs(dir.y) == 1)
                {
                    int2 sideA = current + new int2(dir.x, 0);
                    int2 sideB = current + new int2(0, dir.y);
                    if (obstacles.Contains(sideA) || obstacles.Contains(sideB))
                        continue;
                }

                // Si ya se visitó, continuar.
                if (cameFrom.ContainsKey(next))
                    continue;

                // Registrar de dónde venimos y añadir a la cola.
                cameFrom.TryAdd(next, current);
                frontier.Enqueue(next);
            }
        }

        // Si no se encontró camino, liberar recursos y salir.
        if (!found)
        {
            frontier.Dispose();
            cameFrom.Dispose();
            tmpPath.Dispose();
            path.Clear();
            return false;
        }

        // Reconstruir el camino desde el objetivo hasta el origen.
        int2 p = target;
        while (!math.all(p == start))
        {
            tmpPath.Add(p);
            p = cameFrom[p];
        }
        tmpPath.Add(start);

        // Invertir manualmente para que quede desde el origen al destino.
        for (int i = 0, j = tmpPath.Length - 1; i < j; i++, j--)
        {
            int2 tmp = tmpPath[i];
            tmpPath[i] = tmpPath[j];
            tmpPath[j] = tmp;
        }

        path.Clear();
        for (int i = 0; i < tmpPath.Length; i++)
            path.Add(new MovementPathElement { Cell = tmpPath[i] });

        frontier.Dispose();
        cameFrom.Dispose();
        tmpPath.Dispose();
        return true;
    }
}
