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

    float timer;
    int samples;
    readonly List<Vector3> plantPoints = new List<Vector3>();
    readonly List<Vector3> herbPoints = new List<Vector3>();
    readonly List<Vector3> carnPoints = new List<Vector3>();

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < sampleInterval) return;
        timer = 0f;
        samples++;

        int plantCount = VegetationManager.Instance != null ? VegetationManager.Instance.activeVegetation.Count : 0;
        int herbCount = Herbivore.All.Count;
        int carnCount = Carnivore.All.Count;

        AddPoint(plantsLine, plantPoints, plantCount);
        AddPoint(herbivoresLine, herbPoints, herbCount);
        AddPoint(carnivoresLine, carnPoints, carnCount);
    }

    void AddPoint(LineRenderer lr, List<Vector3> list, float value)
    {
        if (lr == null) return;
        Vector3 point = new Vector3(samples, value * yScale, 0f);
        list.Add(point);
        lr.positionCount = list.Count;
        lr.SetPositions(list.ToArray());
    }
}
