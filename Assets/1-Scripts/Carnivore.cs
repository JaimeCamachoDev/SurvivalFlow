using UnityEngine;
using System.Linq;

/// <summary>
/// Gestiona a los carnívoros: buscan carne o presas, atacan y se mueven
/// imitando un depredador sencillo. Comentarios en español explican cada paso.
/// </summary>
public class Carnivore : MonoBehaviour
{
    public float maxHunger = 100f;
    public float hunger = 100f;
    public float hungerRate = 5f;           // Pérdida de hambre por segundo
    public float hungerDeathThreshold = 0f; // Umbral de muerte
    public float seekThreshold = 50f;       // Empieza a buscar comida por debajo de este valor
    public float calmSpeed = 2.5f;          // Velocidad cuando está satisfecho
    public float runSpeed = 4f;             // Velocidad al tener hambre o perseguir
    public float attackRate = 15f;          // Daño por segundo
    public float eatRate = 20f;             // Nutrición ganada por segundo al comer
    public float wanderChangeInterval = 3f; // Cada cuánto cambia de dirección al vagar
    public float avoidanceRadius = 0.5f;    // Distancia mínima con otros carnívoros
    public float detectionRadius = 6f;      // Radio para detectar presas o carne

    Herbivore targetPrey;                  // Herbívoro seleccionado como presa
    MeatTile targetMeat;                    // Carne en el suelo
    Vector3 wanderDir;                      // Dirección al deambular
    float wanderTimer;                      // Temporizador de cambio de dirección

    void Update()
    {
        hunger -= hungerRate * Time.deltaTime;
        if (hunger <= hungerDeathThreshold)
        {
            Die();
            return;
        }

        bool hungry = hunger <= seekThreshold;      // Necesita comida
        if (hunger >= maxHunger)
        {
            hunger = maxHunger;
            targetPrey = null;
            targetMeat = null;
        }

        // Si tiene hambre y no tiene objetivos válidos, buscar carne o presas
        if (hungry)
        {
            if (targetMeat == null || !targetMeat.isAlive ||
                Vector3.Distance(transform.position, targetMeat.transform.position) > detectionRadius)
                FindMeat();
            if (targetMeat == null && (targetPrey == null ||
                Vector3.Distance(transform.position, targetPrey.transform.position) > detectionRadius))
                FindPrey();
        }

        Vector3 moveDir = Vector3.zero;
        bool isEating = false;
        bool pursuing = targetPrey != null || targetMeat != null; // Para ajustar velocidad

        if (pursuing && targetMeat != null)
        {
            Vector3 toMeat = targetMeat.transform.position - transform.position;
            toMeat.y = 0f;
            if (toMeat.magnitude < 1.5f)
            {
                float eaten = targetMeat.Consume(eatRate * Time.deltaTime);
                hunger = Mathf.Min(hunger + eaten, maxHunger);
                isEating = true;
                if (hunger >= maxHunger || !targetMeat.isAlive)
                {
                    targetMeat = null;
                    pursuing = targetPrey != null;
                }
            }
            else
            {
                moveDir += toMeat.normalized;
            }
        }
        else if (pursuing && targetPrey != null)
        {
            Vector3 toPrey = targetPrey.transform.position - transform.position;
            toPrey.y = 0f;
            if (toPrey.magnitude < 1.5f)
            {
                targetPrey.TakeDamage(attackRate * Time.deltaTime);
                isEating = true;
                if (hunger >= maxHunger || targetPrey == null)
                {
                    targetPrey = null;
                    pursuing = targetMeat != null;
                }
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
            float speed = (hungry || pursuing) ? runSpeed : calmSpeed;
            transform.position += dir * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    // Busca el herbívoro vivo más cercano dentro del radio de detección
    void FindPrey()
    {
        Herbivore[] candidates = FindObjectsByType<Herbivore>(FindObjectsSortMode.None)
            .Where(h => Vector3.Distance(transform.position, h.transform.position) <= detectionRadius)
            .ToArray();
        if (candidates.Length == 0) return;
        targetPrey = candidates
            .OrderBy(h => Vector3.Distance(transform.position, h.transform.position))
            .FirstOrDefault();
    }

    // Busca carne disponible en el suelo
    void FindMeat()
    {
        MeatTile[] meats = FindObjectsByType<MeatTile>(FindObjectsSortMode.None)
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
