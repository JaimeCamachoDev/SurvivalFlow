using UnityEngine;
using System.Linq;

public class Herbivore : MonoBehaviour
{
    public float hunger = 100f;
    public float hungerRate = 5f;
    public float hungerDeathThreshold = 0f;
    public float moveSpeed = 2f;
    public float eatRate = 10f;

    public VegetationTile targetPlant;

    void Update()
    {
        hunger -= hungerRate * Time.deltaTime;

        if (hunger <= hungerDeathThreshold)
        {
            Die();
            return;
        }

        if (targetPlant == null || !targetPlant.isAlive)
            FindNewTarget();

        if (targetPlant != null)
        {
            MoveTowards(targetPlant.transform.position);

            if (Vector3.Distance(transform.position, targetPlant.transform.position) < 1.5f)
            {
                float eaten = targetPlant.Consume(eatRate * Time.deltaTime);
                hunger = Mathf.Min(hunger + eaten, 100f);
            }
        }
    }

    void FindNewTarget()
    {
        if (VegetationManager.Instance == null || VegetationManager.Instance.activeVegetation.Count == 0)
            return;

        targetPlant = VegetationManager.Instance.activeVegetation
            .Where(p => p.isAlive)
            .OrderBy(p => Vector3.Distance(transform.position, p.transform.position))
            .FirstOrDefault();
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0f; // para evitar subir/bajar en Y si el terreno es plano
        if (dir.magnitude > 0.1f)
        {
            transform.position += dir.normalized * moveSpeed * Time.deltaTime;
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}

