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
        FixedString64Bytes name = new FixedString64Bytes();
        name.Append("Herb");
        name.AppendInt(counter);
        return name;
    }
}
