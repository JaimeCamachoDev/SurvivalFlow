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

        // Iterar sobre todos los agentes plantilla existentes.
        foreach (var (transform, agent, gp, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<TemplateAgent>, RefRW<GridPosition>>().WithEntityAccess())
        {
            // Posición actual del agente en celdas.
            int2 current = gp.ValueRO.Cell;

            // Si ya llegó a su objetivo, seleccionar uno nuevo aleatorio.
            if (math.all(current == agent.ValueRO.Target))
            {
                int2 newTarget;
                do
                {
                    newTarget = new int2(rand.NextInt(-bounds.x, bounds.x + 1), rand.NextInt(-bounds.y, bounds.y + 1));
                } while (obstacles.Contains(newTarget));
                agent.ValueRW.Target = newTarget;
            }

            // Obtener el camino hasta el objetivo evitando obstáculos.
            if (!FindPath(current, agent.ValueRO.Target, obstacles, bounds, out var path))
            {
                // Si falla, reintentar con un nuevo objetivo aleatorio.
                int2 newTarget;
                do
                {
                    newTarget = new int2(rand.NextInt(-bounds.x, bounds.x + 1), rand.NextInt(-bounds.y, bounds.y + 1));
                } while (obstacles.Contains(newTarget));
                agent.ValueRW.Target = newTarget;
                if (!FindPath(current, newTarget, obstacles, bounds, out path))
                    continue; // No se encontró camino, saltar a la siguiente entidad.
            }

            // Dibujar la ruta calculada en la vista de escena del editor.
            for (int i = 0; i < path.Length - 1; i++)
            {
                float3 a = new float3(path[i].x, 0f, path[i].y);
                float3 b = new float3(path[i + 1].x, 0f, path[i + 1].y);
                Debug.DrawLine(a, b, Color.cyan);
            }
            
            // Desplazar suavemente al agente hacia la siguiente celda del camino.
            if (path.Length > 1)
            {
                int2 next = path[1];
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
                }
                else
                {
                    // Avanzar una fracción hacia la celda destino.
                    transform.ValueRW.Position = pos + delta / dist * step;
                }
            }
            // Liberar la lista de celdas del camino.
            path.Dispose();
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
