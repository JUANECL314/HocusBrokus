using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Photon.Pun;

public class SettingsMenuControllerPhoton : MonoBehaviour
{
    [Header("UI (asignar en Inspector)")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] Slider mouseSens;
    [SerializeField] Slider gamepadSens;

    [SerializeField] GameObject keyboardPanel;
    [SerializeField] GameObject gamepadPanel;

    // PlayerInput del OWNER (en tu jugador con PhotonView.IsMine)
    private PlayerInput ownerInput;
    private bool searchedOwner = false;

    void Start()
    {
        if (!panelRoot) Debug.LogError("[SettingsMenuPhoton] panelRoot no asignado");
        if (panelRoot) panelRoot.SetActive(false);

        // Aseguramos que inicien apagados
        if (keyboardPanel) keyboardPanel.SetActive(false);
        if (gamepadPanel) gamepadPanel.SetActive(false);

        RefreshUIFromSettings();
    }

    void Update()
    {
        if (Keyboard.current != null)
        {
            // Abrir/cerrar panel principal
            if (Keyboard.current.f10Key.wasPressedThisFrame)
                TogglePanel();

            // --- NUEVO: Activar/desactivar submenús ---
            if (panelRoot != null && panelRoot.activeSelf) // Solo si el menú está abierto
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                    ToggleKeyboardPanel();

                if (Keyboard.current.qKey.wasPressedThisFrame)
                    ToggleGamepadPanel();
            }
        }
    }

    void FindOwnerPlayerInputOnce()
    {
        if (searchedOwner) return;
        searchedOwner = true;

        foreach (var pv in FindObjectsOfType<PhotonView>())
        {
            if (pv.IsMine)
            {
                ownerInput = pv.GetComponent<PlayerInput>();
                break;
            }
        }
    }

    // ==========================
    //  SUBMENÚS (MODIFICADO)
    // ==========================

    public void ToggleKeyboardPanel()
    {
        if (!keyboardPanel) return;
        bool show = !keyboardPanel.activeSelf;
        keyboardPanel.SetActive(show);
    }

    public void ToggleGamepadPanel()
    {
        if (!gamepadPanel) return;
        bool show = !gamepadPanel.activeSelf;
        gamepadPanel.SetActive(show);
    }

    // ---- Botones opcionales para cerrar ----
    public void CloseKeyboardPanel()
    {
        if (keyboardPanel) keyboardPanel.SetActive(false);
    }

    public void CloseGamepadPanel()
    {
        if (gamepadPanel) gamepadPanel.SetActive(false);
    }

    // ---- Botones UI originales (mantener compatibilidad) ----
    public void ShowKeyboardPanel()
    {
        if (keyboardPanel) keyboardPanel.SetActive(true);
    }

    public void ShowGamepadPanel()
    {
        if (gamepadPanel) gamepadPanel.SetActive(true);
    }

    // ==========================

    public void TogglePanel()
    {
        if (!panelRoot) return;

        EnsureEventSystem();

        bool show = !panelRoot.activeSelf;
        panelRoot.SetActive(show);

        if (show)
        {
            FindOwnerPlayerInputOnce();
            if (ownerInput) ownerInput.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            RefreshUIFromSettings();
        }
        else
        {
            PushUIToSettings();
            if (ownerInput) ownerInput.enabled = true;

            // Al cerrar el menú, se apagan ambos submenús
            if (keyboardPanel) keyboardPanel.SetActive(false);
            if (gamepadPanel) gamepadPanel.SetActive(false);
        }
    }

    void RefreshUIFromSettings()
    {
        float ms = 1.5f, gs = 0.7f;
        if (SettingsManager.I)
        {
            ms = SettingsManager.I.MouseSensitivity;
            gs = SettingsManager.I.GamepadSensitivity;
        }
        if (mouseSens) mouseSens.value = ms;
        if (gamepadSens) gamepadSens.value = gs;
    }

    void PushUIToSettings()
    {
        if (!SettingsManager.I) return;
        if (mouseSens) SettingsManager.I.SetMouseSensitivity(mouseSens.value);
        if (gamepadSens) SettingsManager.I.SetGamepadSensitivity(gamepadSens.value);
    }

    public void OnMouseSensChanged(float v) { if (SettingsManager.I) SettingsManager.I.SetMouseSensitivity(v); }
    public void OnGamepadSensChanged(float v) { if (SettingsManager.I) SettingsManager.I.SetGamepadSensitivity(v); }
    public void OnCloseClicked() { TogglePanel(); }

    static void EnsureEventSystem()
    {
        if (!EventSystem.current)
        {
            var go = new GameObject("EventSystem (auto)");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
