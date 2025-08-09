using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controla el comportamiento de un herbívoro: búsqueda de plantas,
/// alimentación hasta saciarse, deambular y reproducción simple.
/// Cada bloque de código está comentado en español para facilitar su comprensión.
/// </summary>
public class Herbivore : MonoBehaviour
{
    public static readonly List<Herbivore> All = new List<Herbivore>();
    static readonly Collider[] predatorBuffer = new Collider[32];
    static readonly Collider[] neighborBuffer = new Collider[32];
    static readonly Collider[] plantCheckBuffer = new Collider[32];
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

    enum HerbivoreState { Wandering, Eating, Fleeing, SeekingMate }
    HerbivoreState state = HerbivoreState.Wandering;

    void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        if (cachedRenderer != null)
            baseColor = cachedRenderer.material.color;
        baseScale = transform.localScale;
        health = maxHealth * 0.2f; // Nacen con 20% de vida
        UpdateScale();
        All.Add(this);
    }

    void OnDestroy()
    {
        All.Remove(this);
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

        // Actualizar enfriamientos y objetivos
        reproductionTimer -= Time.deltaTime;
        if (hunger <= seekThreshold && targetPlant == null)
            FindNewTarget();

        // Determinar el estado actual con prioridades exclusivas
        HerbivoreState newState = HerbivoreState.Wandering;

        // 1. Depredadores cercanos -> huir en dirección opuesta a todos ellos
        int predatorCount = Physics.OverlapSphereNonAlloc(transform.position, predatorDetection, predatorBuffer);
        Vector3 fleeDirection = Vector3.zero;
        bool predatorNearby = false;
        for (int i = 0; i < predatorCount; i++)
        {
            var p = predatorBuffer[i];
            if (p == null || p.GetComponent<Carnivore>() == null) continue;
            predatorNearby = true;
            Vector3 away = transform.position - p.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.001f)
                fleeDirection += away.normalized;
        }
        if (predatorNearby)
        {
            newState = HerbivoreState.Fleeing;
        }
        else
        {
            // 2. Reproducción
            if (hunger >= reproductionThreshold && reproductionTimer <= 0f)
            {
                if (partnerTarget == null || partnerTarget.hunger < reproductionThreshold || partnerTarget.reproductionTimer > 0f)
                {
                    partnerTarget = FindPartner();
                    if (partnerTarget != null && partnerTarget.partnerTarget == null)
                        partnerTarget.partnerTarget = this;
                }
                if (partnerTarget != null)
                    newState = HerbivoreState.SeekingMate;
            }

            // 3. Comer
            if (newState == HerbivoreState.Wandering && targetPlant != null)
                newState = HerbivoreState.Eating;
        }

        state = newState;

        // Movimiento según el estado
        Vector3 moveDir = Vector3.zero;

        switch (state)
        {
            case HerbivoreState.Eating:
                if (targetPlant == null)
                    break;
                Vector3 toPlant = targetPlant.transform.position - transform.position;
                toPlant.y = 0f;
                if (toPlant.magnitude < 1.5f)
                {
                    float eaten = targetPlant.Consume(eatRate * Time.deltaTime);
                    hunger = Mathf.Min(hunger + eaten, maxHunger);
                    health = Mathf.Min(health + eaten, maxHealth);
                    UpdateScale();
                    if (hunger >= maxHunger || !targetPlant.isAlive)
                        targetPlant = null;
                }
                else
                {
                    moveDir = toPlant.normalized;
                }
                break;

            case HerbivoreState.Fleeing:
                if (fleeDirection.sqrMagnitude > 0.001f)
                    moveDir = fleeDirection.normalized;
                break;

            case HerbivoreState.SeekingMate:
                if (partnerTarget == null)
                    break;
                Vector3 toMate = partnerTarget.transform.position - transform.position;
                toMate.y = 0f;
                if (toMate.magnitude < reproductionDistance)
                {
                    ReproduceWith(partnerTarget);
                    partnerTarget = null;
                }
                else
                {
                    moveDir = toMate.normalized;
                }
                break;

            case HerbivoreState.Wandering:
                wanderTimer -= Time.deltaTime;
                if (wanderTimer <= 0f)
                {
                    wanderDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                    wanderTimer = wanderChangeInterval;
                }
                moveDir = wanderDir;
                break;
        }

        // Evitar superposición con otros herbívoros
        if (state != HerbivoreState.Eating)
        {
            int neighborCount = Physics.OverlapSphereNonAlloc(transform.position, avoidanceRadius, neighborBuffer);
            for (int i = 0; i < neighborCount; i++)
            {
                var n = neighborBuffer[i];
                if (n == null || n.gameObject == gameObject) continue;
                if (n.GetComponent<Herbivore>() == null) continue;

                Vector3 away = transform.position - n.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.001f)
                    moveDir += away.normalized;
            }
        }

        // Movimiento final
        if (moveDir.sqrMagnitude > 0.001f)
        {
            float speed = calmSpeed;
            if (state == HerbivoreState.Fleeing || state == HerbivoreState.SeekingMate || (state == HerbivoreState.Eating && hunger <= seekThreshold))
                speed = runSpeed;

            Vector3 dir = moveDir.normalized;
            transform.position += dir * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir);
            ClampToBounds();
        }

        UpdateColor(state);
    }

    // Busca la planta viva más cercana dentro del radio de detección
    void FindNewTarget()
    {
        if (VegetationManager.Instance == null)
            return;
        var plants = VegetationManager.Instance.activeVegetation;
        if (plants == null || plants.Count == 0)
            return;

        float closest = float.MaxValue;
        VegetationTile closestPlant = null;
        for (int i = 0; i < plants.Count; i++)
        {
            var p = plants[i];
            if (p == null || !p.isAlive)
                continue;
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist > detectionRadius)
                continue;
            int count = Physics.OverlapSphereNonAlloc(p.transform.position, predatorDetection, plantCheckBuffer);
            bool danger = false;
            for (int j = 0; j < count; j++)
            {
                if (plantCheckBuffer[j] != null && plantCheckBuffer[j].GetComponent<Carnivore>() != null)
                {
                    danger = true;
                    break;
                }
            }
            if (danger)
                continue;
            if (dist < closest)
            {
                closest = dist;
                closestPlant = p;
            }
        }
        targetPlant = closestPlant;
    }

    // Busca un compañero disponible dentro del radio de búsqueda
    Herbivore FindPartner()
    {
        Herbivore best = null;
        float bestDist = float.MaxValue;
        foreach (var h in All)
        {
            if (h == this) continue;
            if (h.hunger < reproductionThreshold || h.reproductionTimer > 0f) continue;
            float dist = Vector3.Distance(transform.position, h.transform.position);
            if (dist <= reproductionSeekRadius && dist < bestDist)
            {
                bestDist = dist;
                best = h;
            }
        }
        return best;
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
    void UpdateColor(HerbivoreState currentState)
    {
        if (!VisualCueSettings.enableVisualCues || cachedRenderer == null || VisualCueSettings.Instance == null)
            return;

        if (wasHurt)
            cachedRenderer.material.color = VisualCueSettings.Instance.herbivoreInjuredColor;
        else
        {
            switch (currentState)
            {
                case HerbivoreState.SeekingMate:
                    cachedRenderer.material.color = VisualCueSettings.Instance.herbivoreReproducingColor;
                    break;
                case HerbivoreState.Eating:
                    cachedRenderer.material.color = VisualCueSettings.Instance.herbivoreEatingColor;
                    break;
                case HerbivoreState.Fleeing:
                    cachedRenderer.material.color = VisualCueSettings.Instance.herbivoreFleeingColor;
                    break;
                default:
                    cachedRenderer.material.color = baseColor;
                    break;
            }
        }
        wasHurt = false;
    }

    void UpdateScale()
    {
        float t = Mathf.Clamp01(health / maxHealth);
        transform.localScale = baseScale * t;
    }
}

