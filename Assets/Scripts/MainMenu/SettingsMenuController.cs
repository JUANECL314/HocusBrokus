using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;   // ⬅️ Necesario
using Photon.Pun;

public class SettingsMenuController : MonoBehaviour
{
    [Header("UI (asignar en Inspector)")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Slider mouseSens;
    [SerializeField] private Slider gamepadSens;

    [Header("Submenús")]
    [SerializeField] private GameObject keyboardPanel;
    [SerializeField] private GameObject gamepadPanel;

    [Header("Escenas Lobby")]
    [SerializeField] private string[] allowedScenesWithPhoton;

    private PlayerInput localPlayerInput;
    private bool triedFindPlayer = false;

    void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        bool isAllowedScene = false;

        if (allowedScenesWithPhoton != null)
        {
            for (int i = 0; i < allowedScenesWithPhoton.Length; i++)
            {
                if (!string.IsNullOrEmpty(allowedScenesWithPhoton[i]) &&
                    allowedScenesWithPhoton[i] == currentScene)
                {
                    isAllowedScene = true;
                    break;
                }
            }
        }

        if (PhotonNetwork.IsConnected && !isAllowedScene)
        {
            if (panelRoot) panelRoot.SetActive(false);
            enabled = false;
            return;
        }

        if (!panelRoot)
            Debug.LogError("[SettingsMenu] panelRoot no asignado");

        if (panelRoot)
            panelRoot.SetActive(false);

        if (keyboardPanel) keyboardPanel.SetActive(false);
        if (gamepadPanel) gamepadPanel.SetActive(false);

        RefreshUIFromSettings();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Abrir/cerrar menú principal
        if (Keyboard.current.f10Key.wasPressedThisFrame)
            TogglePanel();

        // Solo si el menú está abierto
        if (panelRoot != null && panelRoot.activeSelf)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
                ToggleKeyboardPanel();

            if (Keyboard.current.qKey.wasPressedThisFrame)
                ToggleGamepadPanel();
        }
    }

    void TryFindLocalPlayerInput()
    {
        if (triedFindPlayer) return;
        triedFindPlayer = true;

        localPlayerInput = FindObjectOfType<PlayerInput>();
    }

    // ============================
    //  SUBMENÚS EXCLUSIVOS
    // ============================

    public void ToggleKeyboardPanel()
    {
        if (!keyboardPanel) return;

        bool show = !keyboardPanel.activeSelf;

        if (show && gamepadPanel)
            gamepadPanel.SetActive(false);

        keyboardPanel.SetActive(show);
    }

    public void ToggleGamepadPanel()
    {
        if (!gamepadPanel) return;

        bool show = !gamepadPanel.activeSelf;

        if (show && keyboardPanel)
            keyboardPanel.SetActive(false);

        gamepadPanel.SetActive(show);
    }

    // ---- Mostrar uno específico desde UI ----
    public void ShowKeyboardPanel()
    {
        if (keyboardPanel)
        {
            keyboardPanel.SetActive(true);
            if (gamepadPanel) gamepadPanel.SetActive(false);
        }
    }

    public void ShowGamepadPanel()
    {
        if (gamepadPanel)
        {
            gamepadPanel.SetActive(true);
            if (keyboardPanel) keyboardPanel.SetActive(false);
        }
    }

    // ---- Ocultar desde UI ----
    public void HideKeyboardPanel()
    {
        if (keyboardPanel) keyboardPanel.SetActive(false);
    }

    public void HideGamepadPanel()
    {
        if (gamepadPanel) gamepadPanel.SetActive(false);
    }

    // ============================

    public void TogglePanel()
    {
        if (!panelRoot) return;

        EnsureEventSystem();

        bool show = !panelRoot.activeSelf;
        panelRoot.SetActive(show);

        if (show)
        {
            TryFindLocalPlayerInput();
            if (localPlayerInput) localPlayerInput.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            RefreshUIFromSettings();
        }
        else
        {
            PushUIToSettings();

            // al cerrar, siempre apaga los submenús
            if (keyboardPanel) keyboardPanel.SetActive(false);
            if (gamepadPanel) gamepadPanel.SetActive(false);

            if (localPlayerInput) localPlayerInput.enabled = true;
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

    public void OnMouseSensChanged(float v)
    {
        if (SettingsManager.I) SettingsManager.I.SetMouseSensitivity(v);
    }

    public void OnGamepadSensChanged(float v)
    {
        if (SettingsManager.I) SettingsManager.I.SetGamepadSensitivity(v);
    }

    public void OnCloseClicked()
    {
        TogglePanel();
    }

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
