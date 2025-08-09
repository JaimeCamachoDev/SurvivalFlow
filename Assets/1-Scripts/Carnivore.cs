using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona a los carnívoros: buscan carne o presas, atacan y se mueven
/// imitando un depredador sencillo. Comentarios en español explican cada paso.
/// </summary>
public class Carnivore : MonoBehaviour
{
    public static readonly List<Carnivore> All = new List<Carnivore>();
    static readonly Collider[] neighborBuffer = new Collider[32];
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

    [Header("Reproducción")]
    public GameObject carnivorePrefab;
    public float reproductionThreshold = 80f;
    public float reproductionDistance = 2f;
    public float reproductionCooldown = 25f;
    public float reproductionSeekRadius = 6f;    // Radio para buscar pareja activamente
    public int minOffspring = 1;
    public int maxOffspring = 1;


    Herbivore targetPrey;                  // Herbívoro seleccionado como presa
    MeatTile targetMeat;                    // Carne en el suelo
    Vector3 wanderDir;                      // Dirección al deambular
    float wanderTimer;                      // Temporizador de cambio de dirección
    float reproductionTimer;
    Carnivore partnerTarget;               // Pareja con la que intenta reproducirse
    Renderer cachedRenderer;               // Renderer cacheado para colores
    Color baseColor;                       // Color original del material
    Vector3 baseScale;                     // Escala base para crecimiento
    public float maxHealth = 80f;          // Vida máxima
    public float health;                   // Vida actual
    bool wasHurt;                          // Señal cuando recibe daño
    [Header("Rendimiento")]
    [Tooltip("Tiempo en segundos entre actualizaciones de lógica")]
    [Range(0.02f, 1f)] public float updateInterval = 0.1f;
    float updateTimer;

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
        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval)
            return;
        float dt = updateTimer;
        updateTimer = 0f;

        hunger -= hungerRate * dt;
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
        bool isReproducing = hunger >= reproductionThreshold && reproductionTimer <= 0f;

        if (pursuing && targetMeat != null)
        {
            Vector3 toMeat = targetMeat.transform.position - transform.position;
            toMeat.y = 0f;
            if (toMeat.magnitude < 1.5f)
            {
                float eaten = targetMeat.Consume(eatRate * dt);
                hunger = Mathf.Min(hunger + eaten, maxHunger);
                health = Mathf.Min(health + eaten, maxHealth);
                UpdateScale();
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
                float bite = attackRate * dt;
                targetPrey.TakeDamage(bite);
                hunger = Mathf.Min(hunger + bite, maxHunger);
                health = Mathf.Min(health + bite, maxHealth);
                UpdateScale();
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
            wanderTimer -= dt;
            if (wanderTimer <= 0f)
            {
                wanderDir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                wanderTimer = wanderChangeInterval;
            }
            moveDir += wanderDir;
        }

        if (!isEating)
        {
            int neighborCount = Physics.OverlapSphereNonAlloc(transform.position, avoidanceRadius, neighborBuffer);
            for (int i = 0; i < neighborCount; i++)
            {
                var n = neighborBuffer[i];
                if (n == null || n.gameObject == gameObject) continue;
                if (n.GetComponent<Carnivore>() == null) continue;

                Vector3 away = transform.position - n.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.001f)
                    moveDir += away.normalized;
            }
        }

        // Lógica de reproducción: buscar pareja y acercarse
        reproductionTimer -= dt;
        if (hunger >= reproductionThreshold && reproductionTimer <= 0f)
        {
            if (partnerTarget == null || partnerTarget.hunger < reproductionThreshold || partnerTarget.reproductionTimer > 0f)
            {
                partnerTarget = FindPartner();
                if (partnerTarget != null && partnerTarget.partnerTarget == null)
                    partnerTarget.partnerTarget = this;
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
                    pursuing = true; // Moverse rápido hacia la pareja
                }
            }
        }
        else
        {
            partnerTarget = null;
        }

        UpdateColor(isEating, pursuing, hungry && !pursuing, isReproducing);

        if (moveDir.sqrMagnitude > 0.001f && !isEating)
        {
            Vector3 dir = moveDir.normalized;
            float speed = (hungry || pursuing) ? runSpeed : calmSpeed;
            transform.position += dir * speed * dt;
            transform.rotation = Quaternion.LookRotation(dir);
            ClampToBounds();
        }

        // Lógica de reproducción
        reproductionTimer -= dt;
        if (hunger >= reproductionThreshold && reproductionTimer <= 0f)
        {
            Carnivore partner = FindPartner();
            if (partner != null && partner.hunger >= reproductionThreshold && partner.reproductionTimer <= 0f)
            {
                ReproduceWith(partner);
            }
        }
    }

    // Ajusta el color según el estado
    void UpdateColor(bool isEating, bool pursuing, bool fleeing, bool isReproducing)
    {
        if (!VisualCueSettings.enableVisualCues || cachedRenderer == null || VisualCueSettings.Instance == null)
            return;

        if (wasHurt)
            cachedRenderer.material.color = VisualCueSettings.Instance.carnivoreInjuredColor;       // Herido
        else if (isReproducing)
            cachedRenderer.material.color = VisualCueSettings.Instance.carnivoreReproducingColor;   // Reproducción
        else if (isEating)
            cachedRenderer.material.color = VisualCueSettings.Instance.carnivoreEatingColor;        // Comiendo
        else if (fleeing)
            cachedRenderer.material.color = VisualCueSettings.Instance.carnivoreFleeingColor;       // Huyendo
        else if (pursuing)
            cachedRenderer.material.color = VisualCueSettings.Instance.carnivorePursuingColor;      // Persiguiendo
        else
            cachedRenderer.material.color = baseColor;                                            // Calmado
        wasHurt = false;
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        wasHurt = true;
        UpdateScale();
        if (health <= 0f)
            Die();
    }

    void UpdateScale()
    {
        float t = Mathf.Clamp01(health / maxHealth);
        transform.localScale = baseScale * t;
    }

    // Busca el herbívoro vivo más cercano dentro del radio de detección
    void FindPrey()
    {
        Herbivore best = null;
        float bestDist = float.MaxValue;
        foreach (var h in Herbivore.All)
        {
            float dist = Vector3.Distance(transform.position, h.transform.position);
            if (dist <= detectionRadius && dist < bestDist)
            {
                bestDist = dist;
                best = h;
            }
        }
        targetPrey = best;
    }

    // Busca carne disponible en el suelo
    void FindMeat()
    {
        MeatTile best = null;
        float bestDist = float.MaxValue;
        foreach (var m in MeatTile.All)
        {
            if (!m.isAlive) continue;
            float dist = Vector3.Distance(transform.position, m.transform.position);
            if (dist <= detectionRadius && dist < bestDist)
            {
                bestDist = dist;
                best = m;
            }
        }
        targetMeat = best;
    }

    // Busca un compañero disponible dentro del radio de búsqueda
    Carnivore FindPartner()
    {
        Carnivore best = null;
        float bestDist = float.MaxValue;
        foreach (var c in All)
        {
            if (c == this) continue;
            if (c.hunger < reproductionThreshold || c.reproductionTimer > 0f) continue;
            float dist = Vector3.Distance(transform.position, c.transform.position);
            if (dist <= reproductionSeekRadius && dist < bestDist)
            {
                bestDist = dist;
                best = c;
            }
        }
        return best;
    }

    // Crea nuevas crías y aplica el coste energético
    void ReproduceWith(Carnivore partner)
    {
        if (carnivorePrefab == null) return;
        int offspring = Random.Range(minOffspring, maxOffspring + 1);
        for (int i = 0; i < offspring; i++)
        {
            Vector3 spawnPos = (transform.position + partner.transform.position) / 2f;
            spawnPos += Random.insideUnitSphere * 0.5f;
            spawnPos.y = 0f;
            GameObject child = Instantiate(carnivorePrefab, spawnPos, Quaternion.identity);
            Carnivore baby = child.GetComponent<Carnivore>();
            if (baby != null)
            {
                baby.carnivorePrefab = carnivorePrefab;
                baby.minOffspring = minOffspring;
                baby.maxOffspring = maxOffspring;
                baby.hunger = baby.maxHunger * 0.5f;
            }
        }
        float cost = maxHunger * 0.3f;
        hunger = Mathf.Max(hunger - cost, 0f);
        partner.hunger = Mathf.Max(partner.hunger - cost, 0f);
        reproductionTimer = reproductionCooldown;
        partner.reproductionTimer = partner.reproductionCooldown;
    }

    void Die()
    {
        Destroy(gameObject);
    }

    // Mantiene al carnívoro dentro de los límites del mapa definido por VegetationManager
    void ClampToBounds()
    {
        if (VegetationManager.Instance == null) return;
        Vector2 size = VegetationManager.Instance.areaSize;
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -size.x / 2f, size.x / 2f);
        pos.z = Mathf.Clamp(pos.z, -size.y / 2f, size.y / 2f);
        transform.position = pos;
    }
}
