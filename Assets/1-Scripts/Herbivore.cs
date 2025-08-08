using UnityEngine;
using System.Linq;

public class Herbivore : MonoBehaviour
{
    public float maxHunger = 100f;
    public float hunger = 100f;
    public float hungerRate = 5f;
    public float hungerDeathThreshold = 0f;
    public float seekThreshold = 50f;
    public float moveSpeed = 2f;
    public float eatRate = 10f;
    public float wanderChangeInterval = 3f;
    public float avoidanceRadius = 0.5f;
    public float detectionRadius = 5f;
    public float health = 50f;
    public GameObject meatPrefab;

    public VegetationTile targetPlant;

    Vector3 wanderDir;
    float wanderTimer;

    void Update()
    {
        hunger -= hungerRate * Time.deltaTime;
        if (hunger <= hungerDeathThreshold)
        {
            Die();
            return;
        }

        bool hungry = hunger <= seekThreshold;
        if (hunger >= maxHunger)
            hunger = maxHunger;

        if (!hungry)
            targetPlant = null;

        if (hungry && (targetPlant == null || !targetPlant.isAlive ||
            Vector3.Distance(transform.position, targetPlant.transform.position) > detectionRadius))
            FindNewTarget();

        Vector3 moveDir = Vector3.zero;
        bool isEating = false;

        if (hungry && targetPlant != null)
        {
            Vector3 toPlant = targetPlant.transform.position - transform.position;
            toPlant.y = 0f;

            if (toPlant.magnitude < 1.5f)
            {
                float eaten = targetPlant.Consume(eatRate * Time.deltaTime);
                hunger = Mathf.Min(hunger + eaten, maxHunger);
                isEating = true;
            }
            else
            {
                moveDir += toPlant.normalized;
            }
        }
        else
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                wanderTimer = wanderChangeInterval;
            }
            moveDir += wanderDir;
        }

        if (!isEating)
        {
            Collider[] neighbors = Physics.OverlapSphere(transform.position, avoidanceRadius);
            foreach (var n in neighbors)
            {
                if (n.gameObject == gameObject) continue;
                if (n.GetComponent<Herbivore>() == null) continue;

                Vector3 away = transform.position - n.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.001f)
                    moveDir += away.normalized;
            }
        }

        if (moveDir.sqrMagnitude > 0.001f && !isEating)
        {
            Vector3 dir = moveDir.normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    void FindNewTarget()
    {
        VegetationTile[] candidates = null;

        if (VegetationManager.Instance != null && VegetationManager.Instance.activeVegetation.Count > 0)
        {
            candidates = VegetationManager.Instance.activeVegetation
                .Where(p => p.isAlive && Vector3.Distance(transform.position, p.transform.position) <= detectionRadius)
                .ToArray();
        }
        else
        {
            // Fallback por si el manager aún no se ha inicializado
            candidates = FindObjectsOfType<VegetationTile>()
                .Where(p => p.isAlive && Vector3.Distance(transform.position, p.transform.position) <= detectionRadius)
                .ToArray();
        }

        if (candidates.Length == 0)
            return;

        targetPlant = candidates
            .OrderBy(p => Vector3.Distance(transform.position, p.transform.position))
            .FirstOrDefault();
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
            Die();
    }

    void Die()
    {
        if (meatPrefab != null)
            Instantiate(meatPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}

