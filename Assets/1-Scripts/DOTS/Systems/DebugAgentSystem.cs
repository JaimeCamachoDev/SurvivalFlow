using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// Sistema de movimiento para los agentes de depuración.
[UpdateAfter(typeof(ObstacleRegistrySystem))]
public partial struct DebugAgentSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<GridManager>(out var grid))
            return;

        var obstacles = ObstacleRegistrySystem.Obstacles;

        float2 half = grid.AreaSize * 0.5f;
        int2 bounds = (int2)half;
        var rand = Unity.Mathematics.Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000 + 13));

        foreach (var (transform, agent, gp, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<DebugAgent>, RefRW<GridPosition>>().WithEntityAccess())
        {
            int2 current = gp.ValueRO.Cell;

            if (math.all(current == agent.ValueRO.Target))
            {
                int2 newTarget;
                do
                {
                    newTarget = new int2(rand.NextInt(-bounds.x, bounds.x + 1), rand.NextInt(-bounds.y, bounds.y + 1));
                } while (obstacles.Contains(newTarget));
                agent.ValueRW.Target = newTarget;
            }

            if (!FindPath(current, agent.ValueRO.Target, obstacles, bounds, out var path))
                continue;

            for (int i = 0; i < path.Length - 1; i++)
            {
                float3 a = new float3(path[i].x, 0f, path[i].y);
                float3 b = new float3(path[i + 1].x, 0f, path[i + 1].y);
                Debug.DrawLine(a, b, Color.cyan);
            }

            if (path.Length > 1)
            {
                int2 next = path[1];
                float3 world = new float3(next.x, 0f, next.y);
                float3 pos = transform.ValueRO.Position;
                float step = agent.ValueRO.MoveSpeed * SystemAPI.Time.DeltaTime;
                float3 delta = world - pos;
                float dist = math.length(delta);
                if (dist <= step)
                {
                    transform.ValueRW.Position = world;
                    gp.ValueRW.Cell = next;
                }
                else
                {
                    transform.ValueRW.Position = pos + delta / dist * step;
                }
            }

            path.Dispose();
        }
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
        path.Reverse();

        frontier.Dispose();
        cameFrom.Dispose();
        return true;
    }
}
