using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registra la población de plantas, herbívoros y carnívoros y dibuja su evolución
/// temporal utilizando LineRenderers.
/// </summary>
public class PopulationGraph : MonoBehaviour
{
    public LineRenderer plantsLine;
    public LineRenderer herbivoresLine;
    public LineRenderer carnivoresLine;
    public float sampleInterval = 1f; // Tiempo entre muestras
    public float yScale = 0.1f;       // Escala vertical para las cantidades
    public int maxSamples = 200;      // Muestras visibles en el gráfico

    float timer;
    readonly List<Vector3> plantPoints = new List<Vector3>();
    readonly List<Vector3> herbPoints = new List<Vector3>();
    readonly List<Vector3> carnPoints = new List<Vector3>();

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < sampleInterval) return;
        timer = 0f;

        int plantCount = VegetationManager.Instance != null ? VegetationManager.Instance.activeVegetation.Count : 0;
        int herbCount = Herbivores.All.Count;
        int carnCount = Carnivore.All.Count;

        AddPoint(plantsLine, plantPoints, plantCount);
        AddPoint(herbivoresLine, herbPoints, herbCount);
        AddPoint(carnivoresLine, carnPoints, carnCount);
    }

    void AddPoint(LineRenderer lr, List<Vector3> list, float value)
    {
        if (lr == null) return;

        // Añade una muestra y reajusta los puntos para que siempre estén anclados al origen
        list.Add(new Vector3(0f, value * yScale, 0f));
        if (list.Count > maxSamples)
            list.RemoveAt(0);

        for (int i = 0; i < list.Count; i++)
        {
            Vector3 p = list[i];
            p.x = i;
            list[i] = p;
        }

        lr.positionCount = list.Count;
        lr.SetPositions(list.ToArray());
    }
}
