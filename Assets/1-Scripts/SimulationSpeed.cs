using UnityEngine;

public class SimulationSpeed : MonoBehaviour
{
    [Range(0.1f, 10f)]
    public float timeScale = 1f;

    void Update()
    {
        Time.timeScale = timeScale;

        if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals))
            timeScale = Mathf.Min(timeScale * 2f, 10f);
        if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
            timeScale = Mathf.Max(timeScale * 0.5f, 0.1f);
    }
}
