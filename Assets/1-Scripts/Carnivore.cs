using UnityEngine;
using System.Linq;

public class Carnivore : MonoBehaviour
{
    public float maxHunger = 100f;
    public float hunger = 100f;
    public float hungerRate = 5f;
    public float hungerDeathThreshold = 0f;
    public float seekThreshold = 50f;
    public float moveSpeed = 2.5f;
    public float attackRate = 15f;
    public float eatRate = 20f;
    public float wanderChangeInterval = 3f;
    public float avoidanceRadius = 0.5f;
    public float detectionRadius = 6f;

    Herbivore targetPrey;
    MeatTile targetMeat;
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
        {
            targetPrey = null;
            targetMeat = null;
        }

        if (hungry)
        {
            if (targetMeat == null || !targetMeat.isAlive ||
                Vector3.Distance(transform.position, targetMeat.transform.position) > detectionRadius)
                FindMeat();
            if (targetMeat == null && (targetPrey == null || targetPrey == null ||
                Vector3.Distance(transform.position, targetPrey.transform.position) > detectionRadius))
                FindPrey();
        }

        Vector3 moveDir = Vector3.zero;
        bool isEating = false;

        if (hungry && targetMeat != null)
        {
            Vector3 toMeat = targetMeat.transform.position - transform.position;
            toMeat.y = 0f;
            if (toMeat.magnitude < 1.5f)
            {
                float eaten = targetMeat.Consume(eatRate * Time.deltaTime);
                hunger = Mathf.Min(hunger + eaten, maxHunger);
                isEating = true;
            }
            else
            {
                moveDir += toMeat.normalized;
            }
        }
        else if (hungry && targetPrey != null)
        {
            Vector3 toPrey = targetPrey.transform.position - transform.position;
            toPrey.y = 0f;
            if (toPrey.magnitude < 1.5f)
            {
                targetPrey.TakeDamage(attackRate * Time.deltaTime);
                isEating = true;
            }
            else
            {
                moveDir += toPrey.normalized;
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
                if (n.GetComponent<Carnivore>() == null) continue;

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

    void FindPrey()
    {
        Herbivore[] candidates = FindObjectsOfType<Herbivore>()
            .Where(h => Vector3.Distance(transform.position, h.transform.position) <= detectionRadius)
            .ToArray();
        if (candidates.Length == 0) return;
        targetPrey = candidates
            .OrderBy(h => Vector3.Distance(transform.position, h.transform.position))
            .FirstOrDefault();
    }

    void FindMeat()
    {
        MeatTile[] meats = FindObjectsOfType<MeatTile>()
            .Where(m => m.isAlive && Vector3.Distance(transform.position, m.transform.position) <= detectionRadius)
            .ToArray();
        if (meats.Length == 0) return;
        targetMeat = meats
            .OrderBy(m => Vector3.Distance(transform.position, m.transform.position))
            .FirstOrDefault();
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
