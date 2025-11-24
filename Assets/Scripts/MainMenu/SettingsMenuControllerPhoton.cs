using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Photon.Pun;

public class SettingsMenuControllerPhoton : MonoBehaviour
{
    [Header("UI (asignar en Inspector)")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Slider mouseSens;
    [SerializeField] private Slider gamepadSens;

    [Header("Multiplayer UI (raíz de la UI que se debe ocultar)")]
    [SerializeField] private GameObject multiplayerUIRoot;

    // Player del owner
    private PlayerInput ownerInput;
    private FreeFlyCameraMulti ownerController;

    void Start()
    {
        if (!panelRoot)
            Debug.LogError("[SettingsMenuPhoton] panelRoot no asignado");

        if (panelRoot)
            panelRoot.SetActive(false);

        RefreshUIFromSettings();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            TogglePanel();
    }

    /// <summary>
    /// Busca el PhotonView.IsMine y le saca PlayerInput y FreeFlyCameraMulti.
    /// Es seguro llamarlo muchas veces: sólo hace algo si aún no tiene refs.
    /// </summary>
    void FindOwnerComponents()
    {
        // Si ya tenemos al menos uno, no hace falta volver a buscar
        if (ownerInput != null && ownerController != null)
            return;

        foreach (var pv in FindObjectsOfType<PhotonView>())
        {
            if (!pv.IsMine) continue;

            if (ownerInput == null)
                ownerInput = pv.GetComponent<PlayerInput>();

            if (ownerController == null)
            {
                ownerController = pv.GetComponent<FreeFlyCameraMulti>();
                if (!ownerController)
                    ownerController = pv.GetComponentInChildren<FreeFlyCameraMulti>();
            }

            // si ya tenemos ambos, salimos
            if (ownerInput != null && ownerController != null)
                break;
        }
    }

    public void TogglePanel()
    {
        if (!panelRoot) return;

        EnsureEventSystem();

        bool show = !panelRoot.activeSelf;

        // Mostrar/ocultar panel Settings
        panelRoot.SetActive(show);

        // Ocultar/mostrar UI de multiplayer inverso
        if (multiplayerUIRoot)
            multiplayerUIRoot.SetActive(!show);

        if (show)
        {
            // Intentar encontrar al owner (si aún no existe, simplemente quedará en null)
            FindOwnerComponents();

            // Congelar player (si ya está cargado)
            if (ownerController)
                ownerController.SetFrozen(true);

            // Desactivar input del player
            if (ownerInput)
                ownerInput.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            RefreshUIFromSettings();
        }
        else
        {
            PushUIToSettings();

            // Descongelar player (si ya estaba cargado)
            if (ownerController)
                ownerController.SetFrozen(false);

            if (ownerInput)
                ownerInput.enabled = true;
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

    // Hooks UI
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
