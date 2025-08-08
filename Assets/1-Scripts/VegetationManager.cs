using System.Collections.Generic;
using UnityEngine;

public class VegetationManager : MonoBehaviour
{
    public static VegetationManager Instance;

    public List<VegetationTile> activeVegetation = new List<VegetationTile>();

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public void Register(VegetationTile tile)
    {
        if (!activeVegetation.Contains(tile))
            activeVegetation.Add(tile);
    }

    public void Unregister(VegetationTile tile)
    {
        if (activeVegetation.Contains(tile))
            activeVegetation.Remove(tile);
    }
}
