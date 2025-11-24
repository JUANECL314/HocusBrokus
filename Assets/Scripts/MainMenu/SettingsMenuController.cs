using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;   // ⬅️ NUEVO
using Photon.Pun;

public class SettingsMenuController : MonoBehaviour
{
    [Header("UI (asignar en Inspector)")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Slider mouseSens;
    [SerializeField] private Slider gamepadSens;

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

        // ⬅️ Si Photon está conectado Y NO es una escena permitida → apagar este menú
        if (PhotonNetwork.IsConnected && !isAllowedScene)
        {
            if (panelRoot) panelRoot.SetActive(false);
            enabled = false;
            return;
        }

        // Escena offline normal, o escena especial permitida con Photon
        if (!panelRoot) Debug.LogError("[SettingsMenu] panelRoot no asignado");
        if (panelRoot) panelRoot.SetActive(false);
        RefreshUIFromSettings();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            TogglePanel();
    }

    void TryFindLocalPlayerInput()
    {
        if (triedFindPlayer) return;
        triedFindPlayer = true;

        // Busca un PlayerInput en la escena (el tuyo / único)
        localPlayerInput = FindObjectOfType<PlayerInput>();
        // Si no hay ninguno (Main Menu), se queda null y no se deshabilita nada — está bien.
    }

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
            if (localPlayerInput) localPlayerInput.enabled = true;
            // Aquí puedes relockear el cursor si quieres.
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

    // Hooks de UI
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
