using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Management;    // ⬅ VR detect

public class FreeFlyDebug : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Look Settings")]
    public float mouseSensitivity = 1.5f;
    public float gamepadSensitivity = 0.7f;
    public float maxHeadTilt = 80f;
    public bool invertY = false;

    [Header("References")]
    public Transform characterModel;
    public Transform cameraTransform;
    public Vector3 cameraOffset = Vector3.zero;

    [Header("VR")]
    public float vrTurnSpeed = 120f;   // grados/seg
    private float vrTurnAxis;          // acción Turn (Axis)

    private float yaw, pitch;
    private Vector2 moveInput;
    private Vector2 lookInput;

    void Awake()
    {
        if (!cameraTransform)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam) cameraTransform = cam.transform;
        }
        if (!characterModel) characterModel = transform;

        yaw = characterModel.eulerAngles.y;
        pitch = cameraTransform ? cameraTransform.localEulerAngles.x : 0f;
    }

    void Start()
    {
        LockCursor(true);
        if (cameraTransform) cameraTransform.localPosition = cameraOffset;

        if (SettingsManager.I)
        {
            mouseSensitivity = SettingsManager.I.MouseSensitivity;
            gamepadSensitivity = SettingsManager.I.GamepadSensitivity;
            invertY = SettingsManager.I.InvertY;
            SettingsManager.I.OnChanged += ApplySettings;
        }

        // Si VR está activo, apaga la cámara clásica del prefab
        if (XRGeneralSettings.Instance?.Manager?.activeLoader != null && cameraTransform)
            cameraTransform.gameObject.SetActive(false);
    }

    void Update()
    {
        bool xrActive = XRGeneralSettings.Instance?.Manager?.activeLoader != null;

        if (!xrActive)
        {
            bool usingGamepad = Gamepad.current != null && Gamepad.current.rightStick.IsActuated();
            float sensitivity = usingGamepad ? gamepadSensitivity : mouseSensitivity;

            if (lookInput.sqrMagnitude < 0.0005f) lookInput = Vector2.zero;

            float lookX = lookInput.x * sensitivity;
            float lookY = lookInput.y * sensitivity * (invertY ? 1f : -1f);

            yaw += lookX;
            pitch += lookY;
            pitch = Mathf.Clamp(pitch, -maxHeadTilt, maxHeadTilt);

            if (cameraTransform)
            {
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                cameraTransform.localPosition = cameraOffset;
            }
        }
        else
        {
            // En VR solo yaw con Turn (HMD controla la vista)
            yaw += vrTurnAxis * vrTurnSpeed * Time.deltaTime;
        }

        if (characterModel)
            characterModel.rotation = Quaternion.Euler(0f, yaw, 0f);

        // E/Q para subir/bajar (debug)
        float upDownInput = 0f;
        var kb = Keyboard.current;
        if (kb != null) { if (kb.eKey.isPressed) upDownInput += 1f; if (kb.qKey.isPressed) upDownInput -= 1f; }

        Vector3 desired =
            (characterModel.forward * moveInput.y) +
            (characterModel.right * moveInput.x) +
            (Vector3.up * upDownInput);

        if (desired.sqrMagnitude < 0.001f) desired = Vector3.zero;
        if (desired.sqrMagnitude > 1f) desired.Normalize();

        transform.position += desired * moveSpeed * Time.deltaTime;
    }

    void ApplySettings()
    {
        if (!SettingsManager.I) return;
        mouseSensitivity = SettingsManager.I.MouseSensitivity;
        gamepadSensitivity = SettingsManager.I.GamepadSensitivity;
        invertY = SettingsManager.I.InvertY;
    }

    void OnDestroy()
    {
        if (SettingsManager.I) SettingsManager.I.OnChanged -= ApplySettings;
    }

    // --- Input Callbacks ---
    public void OnMove(InputValue v) => moveInput = v.Get<Vector2>();
    public void OnLook(InputValue v) => lookInput = v.Get<Vector2>();
    public void OnTurn(InputValue v) => vrTurnAxis = v.Get<float>();   // ⬅ NUEVO
    public void OnToggleCursor(InputValue v)
    {
        if (v.isPressed) LockCursor(Cursor.lockState != CursorLockMode.Locked);
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
