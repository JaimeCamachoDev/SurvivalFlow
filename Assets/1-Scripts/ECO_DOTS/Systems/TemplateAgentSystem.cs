using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// Sistema de movimiento para los agentes plantilla.
[UpdateAfter(typeof(ObstacleRegistrySystem))]
public partial struct TemplateAgentSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Necesitamos la información de la grilla para limitar el movimiento.
        if (!SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        // Copiar el registro de obstáculos a una variable local para usarlo en las búsquedas.
        var obstacles = ObstacleRegistrySystem.Obstacles;

        // Calcular los límites de la grilla (mitad del tamaño del área).
        float2 half = grid.AreaSize * 0.5f;
        int2 bounds = (int2)half;

        // Crear un generador aleatorio que dependa del tiempo para variar los caminos.
        var rand = Unity.Mathematics.Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000 + 13));

        float dt = SystemAPI.Time.DeltaTime;

        // Iterar sobre todos los agentes plantilla existentes.
        foreach (var (transform, agent, gp, pathBuffer) in SystemAPI
                     .Query<RefRW<LocalTransform>, RefRW<TemplateAgent>, RefRW<GridPosition>, DynamicBuffer<PathBufferElement>>())
        {
            int2 current = gp.ValueRO.Cell;

            // Asegurar que la ruta contenga la celda actual como punto inicial.
            if (pathBuffer.Length == 0)
            {
                pathBuffer.Add(new PathBufferElement { Cell = current });
                agent.ValueRW.PathIndex = 0;
            }

            // Si se alcanzó el final del camino, esperar o buscar uno nuevo.
            if (agent.ValueRO.PathIndex >= pathBuffer.Length)
            {
                agent.ValueRW.WaitTimer -= dt;
                if (agent.ValueRO.WaitTimer > 0f)
                    continue; // El agente "piensa" antes de decidir.

                int2 newTarget;
                do
                {
                    newTarget = new int2(rand.NextInt(-bounds.x, bounds.x + 1), rand.NextInt(-bounds.y, bounds.y + 1));
                } while (obstacles.Contains(newTarget));

                agent.ValueRW.Target = newTarget;
                agent.ValueRW.WaitTimer = rand.NextFloat(0.5f, 1.5f);

                if (FindPath(current, newTarget, obstacles, bounds, out var newPath))
                {
                    pathBuffer.Clear();
                    for (int i = 0; i < newPath.Length; i++)
                        pathBuffer.Add(new PathBufferElement { Cell = newPath[i] });
                    agent.ValueRW.PathIndex = math.min(1, pathBuffer.Length);
                    newPath.Dispose();
                }
                else
                {
                    agent.ValueRW.PathIndex = pathBuffer.Length; // Reintentar en el próximo frame.
                }
            }

            // Desplazar suavemente al agente hacia la siguiente celda del camino.
            if (agent.ValueRO.PathIndex < pathBuffer.Length)
            {
                int2 next = pathBuffer[agent.ValueRO.PathIndex].Cell;
                float3 world = new float3(next.x, 0f, next.y);
                float3 pos = transform.ValueRO.Position;
                float step = agent.ValueRO.MoveSpeed * dt;
                float3 delta = world - pos;
                float dist = math.length(delta);

                if (dist > 0f)
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(delta / dist, math.up());

                if (dist <= step)
                {
                    transform.ValueRW.Position = world;
                    gp.ValueRW.Cell = next;
                    agent.ValueRW.PathIndex++;
                }
                else
                {
                    transform.ValueRW.Position = pos + delta / dist * step;
                }
            }
        }
    }

    private static bool FindPath(int2 start, int2 target, NativeParallelHashSet<int2> obstacles, int2 bounds, out NativeList<int2> path)
    {
        // Capacidad máxima estimada del grid para reservar memoria.
        var capacity = (bounds.x * 2 + 1) * (bounds.y * 2 + 1);
        var cameFrom = new NativeHashMap<int2, int2>(capacity, Allocator.Temp);
        var frontier = new NativeQueue<int2>(Allocator.Temp);
        path = new NativeList<int2>(Allocator.Temp);

        // Inicializar la búsqueda con la celda de partida.
        frontier.Enqueue(start);
        cameFrom.TryAdd(start, start);

        // Direcciones permitidas (4 ortogonales + 4 diagonales).
        int2[] dirs = new int2[8]
        {
            new int2(1,0), new int2(-1,0), new int2(0,1), new int2(0,-1),
            new int2(1,1), new int2(1,-1), new int2(-1,1), new int2(-1,-1)
        };

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
            for (int i = 0; i < 8; i++)
            {
                int2 dir = dirs[i];
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
            path.Dispose();
            path = default;
            return false;
        }

        // Reconstruir el camino desde el objetivo hasta el origen.
        int2 p = target;
        while (!math.all(p == start))
        {
            path.Add(p);
            p = cameFrom[p];
        }
        path.Add(start);

        // NativeList<T> no dispone de Reverse, así que invertimos manualmente.
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
