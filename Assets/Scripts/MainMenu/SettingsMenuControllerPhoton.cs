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

    [SerializeField] GameObject keyboardPanel;
    [SerializeField] GameObject gamepadPanel;

    // PlayerInput del OWNER (en tu jugador con PhotonView.IsMine)
    [Header("Multiplayer UI (objetos que se deben ocultar)")]
    [SerializeField] private GameObject[] multiplayerUIRoots; // UI_Multiplayer, Mira, etc.

    // Referencias del jugador local (dueño)
    private PlayerInput ownerInput;
    private FreeFlyCameraMulti ownerController;
    private PhotonView ownerView;

    // Es true si este controlador le pertenece al jugador local
    private bool IsLocalController
    {
        get
        {
            // En offline, no hay Photon, así que siempre es local
            if (!PhotonNetwork.IsConnected) return true;
            if (ownerView == null) return false;
            return ownerView.IsMine;
        }
    }

    void Awake()
    {
        // Buscamos el PhotonView más cercano hacia arriba en la jerarquía
        ownerView = GetComponentInParent<PhotonView>();
    }

    void Start()
    {
        // Aseguramos que inicien apagados
        if (keyboardPanel) keyboardPanel.SetActive(false);
        if (gamepadPanel) gamepadPanel.SetActive(false);

        if (!panelRoot)
            Debug.LogError("[SettingsMenuPhoton] panelRoot no asignado", this);

        if (panelRoot)
            panelRoot.SetActive(false);

        // 🔒 SI ES UN PLAYER REMOTO => APAGAR SU UI COMPLETA
        if (!IsLocalController)
        {
            // Apagar cualquier HUD/mira que tenga asignado
            if (multiplayerUIRoots != null)
            {
                foreach (var go in multiplayerUIRoots)
                {
                    if (go) go.SetActive(false);
                }
            }

            // Deshabilitar este Canvas y su GraphicRaycaster para que NO capturen clics
            var canvas = GetComponent<Canvas>();
            if (canvas) canvas.enabled = false;

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster) raycaster.enabled = false;

            // No necesitamos que este script siga activo en el remoto
            return;
        }

        // LOCAL: funciona normal
        RefreshUIFromSettings();
    }

void Update()
{
    // Solo el controlador del jugador local escucha F10
    if (!IsLocalController) return;

    if (Keyboard.current != null)
    {
        // Abrir/cerrar panel principal
        if (Keyboard.current.f10Key.wasPressedThisFrame)
            TogglePanel();

        // --- Mantener submenús ---
        if (panelRoot != null && panelRoot.activeSelf) // Solo si el menú está abierto
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
                ToggleKeyboardPanel();

            if (Keyboard.current.qKey.wasPressedThisFrame)
                ToggleGamepadPanel();
        }
    }
}


    /// <summary>
    /// Saca PlayerInput y FreeFlyCameraMulti del mismo prefab del dueño local.
    /// </summary>
    void FindOwnerComponents()
    {
        if (!IsLocalController) return;

        // Si ya tenemos ambas referencias, nada que hacer
        if (ownerInput != null && ownerController != null)
            return;

        if (ownerView == null)
        {
            // Si por alguna razón no encontramos PhotonView (offline?) salimos sin crashear
            return;
        }

        if (ownerInput == null)
        {
            ownerInput = ownerView.GetComponent<PlayerInput>();
            if (ownerInput == null)
                ownerInput = ownerView.GetComponentInChildren<PlayerInput>();
        }

        if (ownerController == null)
        {
            ownerController = ownerView.GetComponent<FreeFlyCameraMulti>();
            if (ownerController == null)
                ownerController = ownerView.GetComponentInChildren<FreeFlyCameraMulti>();
        }
    }

    // ==========================
    //  SUBMEN�S (MODIFICADO)
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

        // Mostrar/ocultar panel Settings (solo en este player local)
        panelRoot.SetActive(show);

        // Ocultar/mostrar TODA la UI de multiplayer inversamente
        if (multiplayerUIRoots != null)
        {
            foreach (var go in multiplayerUIRoots)
            {
                if (go) go.SetActive(!show);
            }
        }

        if (show)
        {
            // Buscar components del dueño local
            FindOwnerComponents();

            // Congelar al player
            if (ownerController)
                ownerController.SetFrozen(true);

            // Desactivar inputs del player
            if (ownerInput)
                ownerInput.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            RefreshUIFromSettings();
        }
        else
        {
            PushUIToSettings();

            // Al cerrar el men�, se apagan ambos submen�s
            if (keyboardPanel) keyboardPanel.SetActive(false);
            if (gamepadPanel) gamepadPanel.SetActive(false);

            // Descongelar al player
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
