using UnityEngine;

/// <summary>
/// Controla la activación del sistema de modificaciones genéticas y el grado de mutación.
/// </summary>
public class GeneticManager : MonoBehaviour
{
    public static GeneticManager Instance;

    [Header("Genética")]
    public bool geneticsEnabled = false;
    [Range(0f, 1f)] public float mutationStrength = 0.1f;

    void Awake()
    {
        Instance = this;
    }

    public float Mutate(float value)
    {
        return value * (1f + Random.Range(-mutationStrength, mutationStrength));
    }

    public void SetGeneticsEnabled(bool enabled)
    {
        geneticsEnabled = enabled;
        ApplyToAll();
    }

    public void SetMutationStrength(float strength)
    {
        mutationStrength = strength;
        ApplyToAll();
    }

    public void ApplyToAll()
    {
        foreach (var h in Herbivore.All)
            h.ApplyGenetics();
        foreach (var c in Carnivore.All)
            c.ApplyGenetics();
    }
}
