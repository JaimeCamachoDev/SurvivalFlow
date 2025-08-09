using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tile de carne que dejan los herbívoros al morir. Se degrada con el tiempo
/// y puede ser consumido por los carnívoros.
/// </summary>
public class MeatTile : MonoBehaviour
{
    public static readonly List<MeatTile> All = new List<MeatTile>();
    public float nutrition = 50f;   // Cantidad de comida disponible
    public float decayRate = 1f;     // Velocidad a la que se pudre

    [Header("Fertilización")] public float fertilizeInterval = 5f;
    public float fertilizeRadius = 3f;
    public int fertilizePlants = 1;

    float timer;

    public bool isAlive => nutrition > 0f; // Sigue existiendo mientras tenga comida

    void Awake()
    {
        All.Add(this);
    }

    void Update()
    {
        if (nutrition <= 0f)
            return;

        nutrition -= decayRate * Time.deltaTime;
        timer += Time.deltaTime;
        if (timer >= fertilizeInterval)
        {
            timer = 0f;
            VegetationManager.Instance?.FertilizeArea(transform.position, fertilizeRadius, fertilizePlants);
        }

        if (nutrition <= 0f)
        {
            VegetationManager.Instance?.FertilizeArea(transform.position, fertilizeRadius, fertilizePlants);
            Destroy(gameObject);
        }
    }

    // Permite a un carnívoro consumir parte de la carne disponiblen
    public float Consume(float amount)
    {
        float eaten = Mathf.Min(amount, nutrition);
        nutrition -= eaten;
        if (nutrition <= 0f)
        {
            VegetationManager.Instance?.FertilizeArea(transform.position, fertilizeRadius, fertilizePlants);
            Destroy(gameObject);
        }
        return eaten;
    }

    void OnDestroy()
    {
        All.Remove(this);
    }
}
