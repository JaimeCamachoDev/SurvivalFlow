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

        bool TryFindNextStep(int2 start, int2 target, out int2 next)
        {
            next = start;
            float best = float.MaxValue;
            int2[] dirs = new int2[8]
            {
                new int2(1,0), new int2(-1,0), new int2(0,1), new int2(0,-1),
                new int2(1,1), new int2(1,-1), new int2(-1,1), new int2(-1,-1)
            };
            for (int i = 0; i < 8; i++)
            {
                int2 cand = start + dirs[i];
                if (math.abs(cand.x) > bounds.x || math.abs(cand.y) > bounds.y)
                    continue;
                if (obstacles.Contains(cand))
                    continue;
                float dist = math.lengthsq((float2)(target - cand));
                if (dist < best)
                {
                    best = dist;
                    next = cand;
                }
            }
            return best < float.MaxValue;
        }

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

            // Dibujamos el camino hasta el objetivo.
            int2 sim = current;
            for (int i = 0; i < 128 && !math.all(sim == agent.ValueRO.Target); i++)
            {
                if (!TryFindNextStep(sim, agent.ValueRO.Target, out var step))
                    break;
                float3 a = new float3(sim.x, 0f, sim.y);
                float3 b = new float3(step.x, 0f, step.y);
                Debug.DrawLine(a, b, Color.cyan);
                sim = step;
            }

            if (TryFindNextStep(current, agent.ValueRO.Target, out var next))
            {
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
        }
    }
}
