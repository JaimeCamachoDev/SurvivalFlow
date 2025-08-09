using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SimulationSpeed : MonoBehaviour
{
    [Range(0.1f, 10f)]
    public float timeScale = 1f;

    [Header("UI")] public TextMeshProUGUI timeScaleText;
    public Slider timeScaleSlider;

    void Start()
    {
        if (timeScaleSlider != null)
        {
            timeScaleSlider.minValue = 0.1f;
            timeScaleSlider.maxValue = 10f;
            timeScaleSlider.value = timeScale;
            timeScaleSlider.onValueChanged.AddListener(v => timeScale = v);
        }
        UpdateText();
    }

    void Update()
    {
        if (timeScaleSlider != null)
            timeScaleSlider.value = timeScale;

        Time.timeScale = timeScale;

        if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals))
            timeScale = Mathf.Min(timeScale * 2f, 10f);
        if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
            timeScale = Mathf.Max(timeScale * 0.5f, 0.1f);

        UpdateText();
    }

    void UpdateText()
    {
        if (timeScaleText != null)
            timeScaleText.text = $"Velocidad: {timeScale:0.0}x";
    }
}
