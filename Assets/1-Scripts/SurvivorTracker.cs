using UnityEngine;

/// <summary>
/// Lleva un registro de los herbívoros y carnívoros que más tiempo han sobrevivido.
/// </summary>
public static class SurvivorTracker
{
    public struct Record
    {
        public string name;
        public float lifespan;
        public float maxHealth;
        public float maxHunger;
        public float runSpeed;
    }

    public static Record bestHerbivore;
    public static Record bestCarnivore;

    public static void ReportDeath(Herbivore h)
    {
        if (h.lifetime > bestHerbivore.lifespan)
        {
            bestHerbivore = new Record
            {
                name = h.individualName,
                lifespan = h.lifetime,
                maxHealth = h.maxHealth,
                maxHunger = h.maxHunger,
                runSpeed = h.runSpeed
            };
        }
    }

    public static void ReportDeath(Carnivore c)
    {
        if (c.lifetime > bestCarnivore.lifespan)
        {
            bestCarnivore = new Record
            {
                name = c.individualName,
                lifespan = c.lifetime,
                maxHealth = c.maxHealth,
                maxHunger = c.maxHunger,
                runSpeed = c.runSpeed
            };
        }
    }

    public static Transform GetOldestHerbivoreTransform()
    {
        Herbivore oldest = null;
        float max = 0f;
        for (int i = 0; i < Herbivore.All.Count; i++)
        {
            var h = Herbivore.All[i];
            if (h.lifetime > max)
            {
                max = h.lifetime;
                oldest = h;
            }
        }
        return oldest != null ? oldest.transform : null;
    }

    public static Transform GetOldestCarnivoreTransform()
    {
        Carnivore oldest = null;
        float max = 0f;
        for (int i = 0; i < Carnivore.All.Count; i++)
        {
            var c = Carnivore.All[i];
            if (c.lifetime > max)
            {
                max = c.lifetime;
                oldest = c;
            }
        }
        return oldest != null ? oldest.transform : null;
    }
}
