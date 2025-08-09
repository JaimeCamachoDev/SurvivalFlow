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

    void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        if (cachedRenderer != null)
            baseColor = cachedRenderer.material.color;
    }

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

        // Lógica de reproducción: buscar pareja y acercarse
        reproductionTimer -= Time.deltaTime;
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

        UpdateColor(isEating, pursuing, hungry && !pursuing);

        if (moveDir.sqrMagnitude > 0.001f && !isEating)
        {
            Vector3 dir = moveDir.normalized;
            float speed = (hungry || pursuing) ? runSpeed : calmSpeed;
            transform.position += dir * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir);
            ClampToBounds();
        }

        // Lógica de reproducción
        reproductionTimer -= Time.deltaTime;
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
    void UpdateColor(bool isEating, bool pursuing, bool fleeing)
    {
        if (!VisualCueSettings.enableVisualCues || cachedRenderer == null)
            return;

        if (isEating)
            cachedRenderer.material.color = Color.green;      // Comiendo
        else if (fleeing)
            cachedRenderer.material.color = Color.red;        // Huyendo/buscando
        else if (pursuing)
            cachedRenderer.material.color = Color.yellow;     // Persiguiendo presa
        else
            cachedRenderer.material.color = baseColor;        // Calmado
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

    // Busca un compañero disponible dentro del radio de búsqueda
    Carnivore FindPartner()
    {
        Carnivore[] pack = FindObjectsByType<Carnivore>(FindObjectsSortMode.None)
            .Where(c => c != this && c.hunger >= reproductionThreshold && c.reproductionTimer <= 0f &&
                   Vector3.Distance(transform.position, c.transform.position) <= reproductionSeekRadius)
            .ToArray();
        if (pack.Length == 0) return null;
        return pack.OrderBy(c => Vector3.Distance(transform.position, c.transform.position)).FirstOrDefault();
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
