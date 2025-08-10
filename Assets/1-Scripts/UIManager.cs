using UnityEngine;
using TMPro;

/// <summary>
/// Permite ocultar/mostrar la interfaz y abrir un panel de ajustes.
/// Los botones de la escena deben llamar a <see cref="ToggleUI"/> y
/// <see cref="ToggleSettingsPanel"/> respectivamente.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Elementos de HUD")]
    public GameObject[] uiElements; // Elementos que se ocultan

    [Header("Panel de ajustes")]
    public GameObject settingsPanel;

    [Header("Botones")]
    public TextMeshProUGUI toggleButtonText;

    [Header("Cámara")]
    public CameraController cameraController;

    bool hudVisible = true;

    /// <summary>
    /// Activa o desactiva los elementos del HUD listados.
    /// </summary>
    public void ToggleUI()
    {
        hudVisible = !hudVisible;
        for (int i = 0; i < uiElements.Length; i++)
        {
            if (uiElements[i] != null)
                uiElements[i].SetActive(hudVisible);
        }
        if (toggleButtonText != null)
            toggleButtonText.text = hudVisible ? "Ocultar UI" : "Mostrar UI";
    }

    /// <summary>
    /// Muestra u oculta el panel de parámetros del ecosistema.
    /// </summary>
    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void CameraFree()
    {
        if (cameraController != null)
            cameraController.SetFreeMode();
    }

    public void CameraFollowHerbivore()
    {
        if (cameraController != null)
            cameraController.FollowBestHerbivore();
    }

    public void CameraFollowCarnivore()
    {
        if (cameraController != null)
            cameraController.FollowBestCarnivore();
    }
}
