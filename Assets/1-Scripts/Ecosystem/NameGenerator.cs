using UnityEngine;

/// <summary>
/// Genera nombres únicos y sencillos para herbívoros y carnívoros.
/// </summary>
public static class NameGenerator
{
    static int herbCount = 0;
    static int carnCount = 0;

    static readonly string[] syllables =
    {
        "ka","lo","mi","su","ra","te","na","vi","do","le"
    };

    static string BuildName(int count)
    {
        int syllableCount = Random.Range(2,4);
        string name = "";
        for (int i = 0; i < syllableCount; i++)
            name += syllables[Random.Range(0, syllables.Length)];
        return char.ToUpper(name[0]) + name.Substring(1) + count;
    }

    public static string GetHerbivoreName()
    {
        herbCount++;
        return BuildName(herbCount);
    }

    public static string GetCarnivoreName()
    {
        carnCount++;
        return BuildName(carnCount);
    }
}
