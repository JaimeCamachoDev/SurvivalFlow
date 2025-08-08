using UnityEngine;

public class MeatTile : MonoBehaviour
{
    public float nutrition = 50f;
    public float decayRate = 1f;

    public bool isAlive => nutrition > 0f;

    void Update()
    {
        if (nutrition <= 0f)
            return;

        nutrition -= decayRate * Time.deltaTime;
        if (nutrition <= 0f)
            Destroy(gameObject);
    }

    public float Consume(float amount)
    {
        float eaten = Mathf.Min(amount, nutrition);
        nutrition -= eaten;
        if (nutrition <= 0f)
            Destroy(gameObject);
        return eaten;
    }
}
