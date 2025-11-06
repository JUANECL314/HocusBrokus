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

    // PlayerInput del OWNER (en tu jugador con PhotonView.IsMine)
    private PlayerInput ownerInput;
    private bool searchedOwner = false;

    void Start()
    {
        if (!panelRoot) Debug.LogError("[SettingsMenuPhoton] panelRoot no asignado");
        if (panelRoot) panelRoot.SetActive(false);
        RefreshUIFromSettings();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            TogglePanel();
    }

    void FindOwnerPlayerInputOnce()
    {
        if (searchedOwner) return;
        searchedOwner = true;

        // Busca el Player del OWNER (PhotonView.IsMine)
        foreach (var pv in FindObjectsOfType<PhotonView>())
        {
            if (pv.IsMine)
            {
                ownerInput = pv.GetComponent<PlayerInput>();
                break;
            }
        }
        // Si no hay owner aún (lobby/menú), queda null — está bien.
    }

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
