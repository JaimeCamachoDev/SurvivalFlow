using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

/// Genera un campo de costes muy simple (placeholder).
[BurstCompile]
public partial struct FlowFieldSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var e = state.EntityManager.CreateEntity();
        state.EntityManager.AddBuffer<FlowFieldCost>(e);
    }

    public void OnUpdate(ref SystemState state)
    {
        int width = 64;
        int height = 64;
        var buffer = SystemAPI.GetSingletonBuffer<FlowFieldCost>();
        buffer.ResizeUninitialized(width * height);

        int2 goal = new int2(width / 2, height / 2);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int idx = x + y * width;
                buffer[idx] = new FlowFieldCost
                {
                    Value = math.abs(x - goal.x) + math.abs(y - goal.y)
                };
            }
    }
}

