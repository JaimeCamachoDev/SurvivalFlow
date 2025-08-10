using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct NeedUpdateSystem : ISystem
{
    const float HUNGER_RATE = 0.01f;
    const float THIRST_RATE = 0.02f;
    const float SLEEP_RATE = 0.005f;

    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (needs, tick) in
                 SystemAPI.Query<RefRW<Needs>, RefRW<NeedTick>>())
        {
            tick.ValueRW.TimeUntilNext -= dt;
            if (tick.ValueRW.TimeUntilNext > 0f)
                continue;

            tick.ValueRW.TimeUntilNext += tick.ValueRO.Period;

            needs.ValueRW.Hunger = math.clamp(needs.ValueRO.Hunger + HUNGER_RATE, 0f, 1f);
            needs.ValueRW.Thirst = math.clamp(needs.ValueRO.Thirst + THIRST_RATE, 0f, 1f);
            needs.ValueRW.Sleep = math.clamp(needs.ValueRO.Sleep + SLEEP_RATE, 0f, 1f);
        }
    }
}
