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
        VegetationTile[] candidates = null;

        if (VegetationManager.Instance != null && VegetationManager.Instance.activeVegetation.Count > 0)
        {
            candidates = VegetationManager.Instance.activeVegetation
                .Where(p => p.isAlive)
                .ToArray();
        }
        else
        {
            // Fallback por si el manager aún no se ha inicializado
            candidates = FindObjectsOfType<VegetationTile>()
                .Where(p => p.isAlive)
                .ToArray();
        }

        if (candidates.Length == 0)
            return;

        targetPlant = candidates
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

