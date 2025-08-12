using Unity.Collections;

/// <summary>
/// Genera nombres sencillos para herbívoros DOTS.
/// </summary>
public static class HerbivoreNameGenerator
{
    static int counter;

    public static FixedString64Bytes NextName()
    {
        counter++;
        return new FixedString64Bytes($"Herb{counter}");
    }
}
