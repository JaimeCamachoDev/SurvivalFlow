using UnityEngine;

/// <summary>
/// Tile de carne que dejan los herbívoros al morir. Se degrada con el tiempo
/// y puede ser consumido por los carnívoros.
/// </summary>
public class MeatTile : MonoBehaviour
{
    public float nutrition = 50f;   // Cantidad de comida disponible
    public float decayRate = 1f;     // Velocidad a la que se pudre

    public bool isAlive => nutrition > 0f; // Sigue existiendo mientras tenga comida

    void Update()
    {
        if (nutrition <= 0f)
            return;

        nutrition -= decayRate * Time.deltaTime;
        if (nutrition <= 0f)
            Destroy(gameObject);
    }

    // Permite a un carnívoro consumir parte de la carne disponible

    public float Consume(float amount)
    {
        float eaten = Mathf.Min(amount, nutrition);
        nutrition -= eaten;
        if (nutrition <= 0f)
            Destroy(gameObject);
        return eaten;
    }
}
