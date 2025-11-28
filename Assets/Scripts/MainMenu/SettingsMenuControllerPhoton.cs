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
    [SerializeField] GameObject guidePanel; // NUEVO PANEL GUÍA

    // NUEVAS IMÁGENES DE GUÍA
    [SerializeField] private GameObject fuegoImage;
    [SerializeField] private GameObject vientoImage;
    [SerializeField] private GameObject tierraImage;
    [SerializeField] private GameObject aguaImage;

    [Header("Multiplayer UI (objetos que se deben ocultar)")]
    [SerializeField] private GameObject[] multiplayerUIRoots;

    private PlayerInput ownerInput;
    private FreeFlyCameraMulti ownerController;
    private PhotonView ownerView;

    private bool IsLocalController
    {
        get
        {
            if (!PhotonNetwork.IsConnected) return true;
            if (ownerView == null) return false;
            return ownerView.IsMine;
        }
    }

    void Awake()
    {
        ownerView = GetComponentInParent<PhotonView>();
    }

    void Start()
    {
        if (keyboardPanel) keyboardPanel.SetActive(false);
        if (gamepadPanel) gamepadPanel.SetActive(false);
        if (guidePanel) guidePanel.SetActive(false);

        if (!panelRoot)
            Debug.LogError("[SettingsMenuPhoton] panelRoot no asignado", this);

        if (panelRoot)
            panelRoot.SetActive(false);

        if (!IsLocalController)
        {
            if (multiplayerUIRoots != null)
            {
                foreach (var go in multiplayerUIRoots)
                {
                    if (go) go.SetActive(false);
                }
            }

            var canvas = GetComponent<Canvas>();
            if (canvas) canvas.enabled = false;

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster) raycaster.enabled = false;

            return;
        }

        RefreshUIFromSettings();
    }

    void Update()
    {
        if (!IsLocalController) return;

        if (Keyboard.current != null)
        {
            // 🔵 Guía puede abrirse siempre
            if (Keyboard.current.gKey.wasPressedThisFrame)
                ToggleGuidePanel();

            // Settings con F10
            if (Keyboard.current.f10Key.wasPressedThisFrame)
                TogglePanel();

            // Submenús solo si settings está activo
            if (panelRoot != null && panelRoot.activeSelf)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                    ToggleKeyboardPanel();

                if (Keyboard.current.qKey.wasPressedThisFrame)
                    ToggleGamepadPanel();
            }
        }
    }

    void FindOwnerComponents()
    {
        if (!IsLocalController) return;
        if (ownerInput != null && ownerController != null) return;

        if (ownerView == null) return;

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
    //  SUBMENÚS
    // ==========================

    public void ToggleKeyboardPanel()
    {
        if (!keyboardPanel) return;
        keyboardPanel.SetActive(!keyboardPanel.activeSelf);
    }

    public void ToggleGamepadPanel()
    {
        if (!gamepadPanel) return;
        gamepadPanel.SetActive(!gamepadPanel.activeSelf);
    }

    // ============================================================
    //  PANEL GUÍA (FUNCIONA IGUAL QUE SETTINGS)
    // ============================================================

    public void ToggleGuidePanel()
    {
        if (!guidePanel) return;

        bool show = !guidePanel.activeSelf;
        guidePanel.SetActive(show);

        FindOwnerComponents();

        if (!IsLocalController) return;

        if (show)
        {
            if (ownerController) ownerController.SetFrozen(true);
            if (ownerInput) ownerInput.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (ownerController) ownerController.SetFrozen(false);
            if (ownerInput) ownerInput.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ShowGuidePanel()
    {
        if (guidePanel) guidePanel.SetActive(true);
    }

    public void CloseGuidePanel()
    {
        if (guidePanel) guidePanel.SetActive(false);
    }

    // ==========================
    //  SETTINGS MENU
    // ==========================

    public void TogglePanel()
    {
        if (!panelRoot) return;

        EnsureEventSystem();

        bool show = !panelRoot.activeSelf;

        panelRoot.SetActive(show);

        if (multiplayerUIRoots != null)
        {
            foreach (var go in multiplayerUIRoots)
            {
                if (go) go.SetActive(!show);
            }
        }

        FindOwnerComponents();

        if (show)
        {
            if (ownerController)
                ownerController.SetFrozen(true);

            if (ownerInput)
                ownerInput.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            RefreshUIFromSettings();
        }
        else
        {
            PushUIToSettings();

            if (keyboardPanel) keyboardPanel.SetActive(false);
            if (gamepadPanel) gamepadPanel.SetActive(false);

            // Guía NO se cierra al cerrar settings

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

    // ============================================================
    //  IMÁGENES DEL PANEL GUÍA
    // ============================================================

    private void HideAllGuideImages()
    {
        if (fuegoImage) fuegoImage.SetActive(false);
        if (vientoImage) vientoImage.SetActive(false);
        if (tierraImage) tierraImage.SetActive(false);
        if (aguaImage) aguaImage.SetActive(false);
    }

    private void ShowGuideImage(GameObject img)
    {
        HideAllGuideImages();
        if (img) img.SetActive(true);
    }

    public void ShowFuego() => ShowGuideImage(fuegoImage);
    public void ShowViento() => ShowGuideImage(vientoImage);
    public void ShowTierra() => ShowGuideImage(tierraImage);
    public void ShowAgua() => ShowGuideImage(aguaImage);

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
