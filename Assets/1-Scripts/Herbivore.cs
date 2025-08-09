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
    public float calmSpeed = 1.5f;              // Velocidad cuando está relajado
    public float runSpeed = 3f;                 // Velocidad al tener hambre o ser perseguido
    public float eatRate = 10f;                 // Cantidad de planta que consume por segundo
    public float wanderChangeInterval = 3f;     // Cada cuánto cambia de dirección al vagar
    public float avoidanceRadius = 0.5f;        // Distancia mínima con otros herbívoros
    public float detectionRadius = 5f;          // Radio para detectar comida
    public float predatorDetection = 6f;        // Radio para detectar depredadores
    public float maxHealth = 50f;            // Vida máxima
    public float health;                     // Vida actual
    public GameObject meatPrefab;                // Prefab que deja al morir

    [Header("Reproducción")]
    public GameObject herbivorePrefab;           // Prefab de nuevas crías
    public float reproductionThreshold = 80f;    // Hambre necesaria para reproducirse
    public float reproductionDistance = 2f;      // Distancia para encontrar pareja
    public float reproductionCooldown = 20f;     // Tiempo entre reproducciones
    public float reproductionSeekRadius = 6f;    // Radio para buscar pareja activamente
    public int minOffspring = 1;
    public int maxOffspring = 1;

    public VegetationTile targetPlant;           // Planta objetivo actual

    Vector3 wanderDir;                           // Dirección de deambular
    float wanderTimer;                           // Temporizador de cambio de dirección
    float reproductionTimer;                     // Controla el enfriamiento de reproducción
    Herbivore partnerTarget;                     // Pareja con la que intenta reproducirse
    Renderer cachedRenderer;                     // Renderer cacheado para cambiar color
    Color baseColor;                             // Color original
    Vector3 baseScale;                          // Escala base para crecimiento
    bool wasHurt;                               // Señal cuando recibe daño

    void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        if (cachedRenderer != null)
            baseColor = cachedRenderer.material.color;
        baseScale = transform.localScale;
        health = maxHealth * 0.2f; // Nacen con 20% de vida
        UpdateScale();
    }

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
        bool isRunning = false;                // Indica si debe ir más rápido
        bool isReproducing = hunger >= reproductionThreshold && reproductionTimer <= 0f;

        if (targetPlant != null)
        {
            Vector3 toPlant = targetPlant.transform.position - transform.position;
            toPlant.y = 0f;

            if (toPlant.magnitude < 1.5f)
            {
                float eaten = targetPlant.Consume(eatRate * Time.deltaTime);
                hunger = Mathf.Min(hunger + eaten, maxHunger);
                health = Mathf.Min(health + eaten, maxHealth);
                UpdateScale();
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

            // Comprobar depredadores cercanos para huir
            Collider[] predators = Physics.OverlapSphere(transform.position, predatorDetection);
            foreach (var p in predators)
            {
                if (p.GetComponent<Carnivore>() == null) continue;

                Vector3 away = transform.position - p.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.001f)
                    moveDir += away.normalized;
                isRunning = true; // Al detectar un depredador, aumenta la velocidad
            }
        }

        // Determinar si debe moverse rápido: hambre o depredadores
        if (hunger <= seekThreshold)
            isRunning = true;

        // Lógica de reproducción: buscar pareja y dirigirse hacia ella
        reproductionTimer -= Time.deltaTime;
        if (hunger >= reproductionThreshold && reproductionTimer <= 0f)
        {
            if (partnerTarget == null || partnerTarget.hunger < reproductionThreshold || partnerTarget.reproductionTimer > 0f)
            {
                partnerTarget = FindPartner();
                if (partnerTarget != null && partnerTarget.partnerTarget == null)
                    partnerTarget.partnerTarget = this; // Asegurar que ambos se buscan
            }

            if (partnerTarget != null)
            {
                Vector3 toMate = partnerTarget.transform.position - transform.position;
                toMate.y = 0f;
                if (toMate.magnitude < reproductionDistance)
                {
                    ReproduceWith(partnerTarget);
                    partnerTarget = null;
                }
                else
                {
                    moveDir += toMate.normalized;
                    isRunning = true; // Moverse más rápido para alcanzar a la pareja
                }
            }
        }
        else
        {
            partnerTarget = null;
        }

        UpdateColor(isEating, isRunning, isReproducing);

        if (moveDir.sqrMagnitude > 0.001f && !isEating)
        {
            Vector3 dir = moveDir.normalized;
            float speed = isRunning ? runSpeed : calmSpeed;
            transform.position += dir * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir);
            ClampToBounds();
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

    // Busca un compañero disponible dentro del radio de búsqueda
    Herbivore FindPartner()
    {
        Herbivore[] herd = FindObjectsByType<Herbivore>(FindObjectsSortMode.None)
            .Where(h => h != this && h.hunger >= reproductionThreshold && h.reproductionTimer <= 0f &&
                   Vector3.Distance(transform.position, h.transform.position) <= reproductionSeekRadius)
            .ToArray();
        if (herd.Length == 0) return null;
        return herd.OrderBy(h => Vector3.Distance(transform.position, h.transform.position)).FirstOrDefault();
    }

    // Instancia una nueva cría y reduce el hambre de los padres
    void ReproduceWith(Herbivore partner)
    {
        if (herbivorePrefab == null) return;

        int offspring = Random.Range(minOffspring, maxOffspring + 1);
        for (int i = 0; i < offspring; i++)
        {
            Vector3 spawnPos = (transform.position + partner.transform.position) / 2f;
            spawnPos += Random.insideUnitSphere * 0.5f;
            spawnPos.y = 0f;
            GameObject child = Instantiate(herbivorePrefab, spawnPos, Quaternion.identity);
            Herbivore baby = child.GetComponent<Herbivore>();
            if (baby != null)
            {
                baby.hunger = baby.maxHunger * 0.5f;
                baby.herbivorePrefab = herbivorePrefab;
                baby.minOffspring = minOffspring;
                baby.maxOffspring = maxOffspring;
            }
        }

        float cost = maxHunger * 0.3f;
        hunger = Mathf.Max(hunger - cost, 0f);
        partner.hunger = Mathf.Max(partner.hunger - cost, 0f);
        reproductionTimer = reproductionCooldown;
        partner.reproductionTimer = partner.reproductionCooldown;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        wasHurt = true;
        UpdateScale();
        if (health <= 0f)
            Die();
    }

    void Die()
    {
        if (meatPrefab != null)
            Instantiate(meatPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    // Mantiene al herbívoro dentro de los límites definidos por el VegetationManager
    void ClampToBounds()
    {
        if (VegetationManager.Instance == null) return;
        Vector2 size = VegetationManager.Instance.areaSize;
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -size.x / 2f, size.x / 2f);
        pos.z = Mathf.Clamp(pos.z, -size.y / 2f, size.y / 2f);
        transform.position = pos;
    }

    // Cambia el color según el estado actual
    void UpdateColor(bool isEating, bool isRunning, bool isReproducing)
    {
        if (!VisualCueSettings.enableVisualCues || cachedRenderer == null || VisualCueSettings.Instance == null)
            return;

        if (wasHurt)
            cachedRenderer.material.color = VisualCueSettings.Instance.herbivoreInjuredColor;        // Herido
        else if (isReproducing)
            cachedRenderer.material.color = VisualCueSettings.Instance.herbivoreReproducingColor;    // Reproducción
        else if (isEating)
            cachedRenderer.material.color = VisualCueSettings.Instance.herbivoreEatingColor;         // Comiendo
        else if (isRunning)
            cachedRenderer.material.color = VisualCueSettings.Instance.herbivoreFleeingColor;        // Huyendo
        else
            cachedRenderer.material.color = baseColor;                                                // Tranquilo
        wasHurt = false;
    }

    void UpdateScale()
    {
        float t = Mathf.Clamp01(health / maxHealth);
        transform.localScale = baseScale * t;
    }
}

