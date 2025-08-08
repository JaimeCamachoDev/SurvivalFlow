using UnityEngine;
using System.Linq;

/// <summary>
/// Controla el comportamiento de un herbívoro: búsqueda de plantas,
/// alimentación hasta saciarse, deambular y reproducción simple.
/// Cada bloque de código está comentado en español para facilitar su comprensión.
/// </summary>
public class Herbivore : MonoBehaviour
{
    public float maxHunger = 100f;
    public float hunger = 100f;

    public float hungerRate = 5f;               // Velocidad a la que pierde hambre
    public float hungerDeathThreshold = 0f;      // Umbral de muerte por hambre
    public float seekThreshold = 50f;            // Por debajo de este porcentaje busca comida
    public float moveSpeed = 2f;
    public float eatRate = 10f;
    public float wanderChangeInterval = 3f;
    public float avoidanceRadius = 0.5f;
    public float detectionRadius = 5f;
    public float health = 50f;                   // Vida del herbívoro
    public GameObject meatPrefab;                // Prefab que deja al morir

    [Header("Reproducción")]
    public GameObject herbivorePrefab;           // Prefab de nuevas crías
    public float reproductionThreshold = 80f;    // Hambre necesaria para reproducirse
    public float reproductionDistance = 2f;      // Distancia para encontrar pareja
    public float reproductionCooldown = 20f;     // Tiempo entre reproducciones

    public VegetationTile targetPlant;           // Planta objetivo actual

    Vector3 wanderDir;                           // Dirección de deambular
    float wanderTimer;                           // Temporizador de cambio de dirección
    float reproductionTimer;                     // Controla el enfriamiento de reproducción

    Vector3 wanderDir;
    float wanderTimer;

    void Update()
    {
        // Actualizar hambre y comprobar muerte
        hunger -= hungerRate * Time.deltaTime;
        if (hunger <= hungerDeathThreshold)
        {
            Die();
            return;
        }

        if (hunger >= maxHunger)
            hunger = maxHunger;

        // Buscar una nueva planta solo si estamos por debajo del umbral y no tenemos objetivo
        if (hunger <= seekThreshold && targetPlant == null)
            FindNewTarget();

        Vector3 moveDir = Vector3.zero;
        bool isEating = false;

        if (targetPlant != null)
        {
            Vector3 toPlant = targetPlant.transform.position - transform.position;
            toPlant.y = 0f;

            if (toPlant.magnitude < 1.5f)
            {
                float eaten = targetPlant.Consume(eatRate * Time.deltaTime);
                hunger = Mathf.Min(hunger + eaten, maxHunger);
                isEating = true;
                // Si estamos llenos o la planta murió, liberamos el objetivo
                if (hunger >= maxHunger || !targetPlant.isAlive)
                    targetPlant = null;
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

        // Lógica de reproducción
        reproductionTimer -= Time.deltaTime;
        if (hunger >= reproductionThreshold && reproductionTimer <= 0f)
        {
            Herbivore partner = FindPartner();
            if (partner != null && partner.hunger >= reproductionThreshold && partner.reproductionTimer <= 0f)
            {
                ReproduceWith(partner);
            }
        }

        if (moveDir.sqrMagnitude > 0.001f && !isEating)
        {
            Vector3 dir = moveDir.normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    // Busca la planta viva más cercana dentro del radio de detección
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
            // Búsqueda de respaldo en caso de que el manager no esté listo
            candidates = FindObjectsByType<VegetationTile>(FindObjectsSortMode.None)

                .Where(p => p.isAlive && Vector3.Distance(transform.position, p.transform.position) <= detectionRadius)
                .ToArray();
        }

        if (candidates.Length == 0)
            return;

        targetPlant = candidates
            .OrderBy(p => Vector3.Distance(transform.position, p.transform.position))
            .FirstOrDefault();
    }

    // Busca un compañero para reproducirse dentro del radio especificado
    Herbivore FindPartner()
    {
        Herbivore[] herd = FindObjectsByType<Herbivore>(FindObjectsSortMode.None)
            .Where(h => h != this && Vector3.Distance(transform.position, h.transform.position) <= reproductionDistance)
            .ToArray();
        if (herd.Length == 0) return null;
        return herd.OrderBy(h => Vector3.Distance(transform.position, h.transform.position)).FirstOrDefault();
    }

    // Instancia una nueva cría y reduce el hambre de los padres
    void ReproduceWith(Herbivore partner)
    {
        if (herbivorePrefab == null) return;

        Vector3 spawnPos = (transform.position + partner.transform.position) / 2f;
        GameObject child = Instantiate(herbivorePrefab, spawnPos, Quaternion.identity);
        Herbivore baby = child.GetComponent<Herbivore>();
        if (baby != null)
            baby.hunger = baby.maxHunger * 0.5f; // La cría empieza medio hambrienta

        // Coste energético para los padres
        hunger *= 0.5f;
        partner.hunger *= 0.5f;
        reproductionTimer = reproductionCooldown;
        partner.reproductionTimer = partner.reproductionCooldown;
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

