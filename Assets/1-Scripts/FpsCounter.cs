using UnityEngine;
using TMPro;

/// <summary>
/// Muestra los FPS actuales en pantalla.
/// </summary>
public class FpsCounter : MonoBehaviour
{
    public TextMeshProUGUI display;
    float timer;
    int frames;

    void Update()
    {
        frames++;
        timer += Time.unscaledDeltaTime;
        if (timer >= 1f)
        {
            if (display != null)
                display.text = $"{frames / timer:0} FPS";
            frames = 0;
            timer = 0f;
        }
    }
}
