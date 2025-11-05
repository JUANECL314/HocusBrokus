using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Management;          // ⬅ VR detect
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class FreeFlyCameraMulti : MonoBehaviourPun
{
    [Header("Mode")]
    [Tooltip("F1 alterna en runtime. Cuando está desactivado, usa movimiento normal con física.")]
    public bool debugFreeFly = false;

    [Header("Movement (Normal)")]
    public float walkSpeed = 5f;
    public float jumpForce = 5f;
    public LayerMask groundMask = ~0;
    public float groundCheckDistance = 0.2f;

    [Header("Movement (Free-Fly Debug)")]
    public float flySpeed = 8f;

    [Header("Look")]
    public float mouseSensitivity = 1.5f;
    public float gamepadSensitivity = 0.7f; // menor para stick
    public float maxHeadTilt = 80f;
    public bool invertY = false;

    [Header("References")]
    public Transform characterModel;  // yaw (cuerpo)
    public Transform cameraTransform; // pitch (cámara clásica)
    public Vector3 cameraOffset = Vector3.zero;

    // ======== VR ========
    [Header("VR")]
    [Tooltip("Velocidad de giro en VR (stick derecho X).")]
    public float vrTurnSpeed = 120f;   // grados/seg
    private float vrTurnAxis;          // valor -1..1 de la acción Turn
    // =====================

    private Rigidbody rb;
    private float yaw, pitch;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool isLocalMode;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!cameraTransform)
            cameraTransform = GetComponentInChildren<Camera>(true)?.transform;
        if (!characterModel) characterModel = transform;
    }

    void Start()
    {
        isLocalMode = !PhotonNetwork.IsConnected;

        if (SettingsManager.I)
        {
            mouseSensitivity = SettingsManager.I.MouseSensitivity;
            gamepadSensitivity = SettingsManager.I.GamepadSensitivity;
            invertY = SettingsManager.I.InvertY;
            SettingsManager.I.OnChanged += ApplySettings;
        }

        if (isLocalMode || photonView.IsMine) ActivateCamera();
        else { DeactivateCamera(); }

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        yaw = characterModel.eulerAngles.y;
        pitch = cameraTransform ? cameraTransform.localEulerAngles.x : 0f;

        LockCursor(true);
        if (cameraTransform) cameraTransform.localPosition = cameraOffset;

        // Si VR está activo, apaga la cámara clásica para evitar doble cámara
        if (XRGeneralSettings.Instance?.Manager?.activeLoader != null && cameraTransform)
            cameraTransform.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isLocalMode && !photonView.IsMine) return;

        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            debugFreeFly = !debugFreeFly;

        bool xrActive = XRGeneralSettings.Instance?.Manager?.activeLoader != null;

        if (!xrActive)
        {
            // --- Look clásico (mouse/gamepad) ---
            bool usingGamepad = Gamepad.current != null && Gamepad.current.rightStick.IsActuated();
            float sens = usingGamepad ? gamepadSensitivity : mouseSensitivity;

            if (lookInput.sqrMagnitude < 0.0005f) lookInput = Vector2.zero;

            float lookX = lookInput.x * sens;
            float lookY = lookInput.y * sens * (invertY ? 1f : -1f);

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
            // --- VR: solo giramos yaw con la acción Turn (el HMD maneja pitch/roll) ---
            yaw += vrTurnAxis * vrTurnSpeed * Time.deltaTime;
        }

        if (characterModel)
            characterModel.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Free-fly vertical (E/Q) solo en modo debug
        if (debugFreeFly)
        {
            float upDown = 0f;
            var kb = Keyboard.current;
            if (kb != null) { if (kb.eKey.isPressed) upDown += 1f; if (kb.qKey.isPressed) upDown -= 1f; }

            Vector3 desired =
                characterModel.forward * moveInput.y +
                characterModel.right * moveInput.x +
                Vector3.up * upDown;

            if (desired.sqrMagnitude < 0.001f) desired = Vector3.zero;
            if (desired.sqrMagnitude > 1f) desired.Normalize();

            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            transform.position += desired * flySpeed * Time.deltaTime;
        }
        else
        {
            if (!rb.useGravity) rb.useGravity = true;
        }
    }

    void FixedUpdate()
    {
        if (!isLocalMode && !photonView.IsMine) return;
        if (debugFreeFly) return;

        Vector3 wishDir = (characterModel.forward * moveInput.y) + (characterModel.right * moveInput.x);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        Vector3 vel = rb.linearVelocity;
        Vector3 targetXZ = wishDir * walkSpeed;
        rb.linearVelocity = new Vector3(targetXZ.x, vel.y, targetXZ.z);

        if (jumpPressed && IsGrounded())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
        jumpPressed = false;
    }

    bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.05f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.05f, groundMask, QueryTriggerInteraction.Ignore);
    }

    // ===== Input Actions =====
    public void OnMove(InputValue v) => moveInput = v.Get<Vector2>();
    public void OnLook(InputValue v) => lookInput = v.Get<Vector2>();
    public void OnJump(InputValue v) { if (v.isPressed) jumpPressed = true; }
    public void OnTurn(InputValue v) => vrTurnAxis = v.Get<float>();      // ⬅ NUEVA acción para VR
    public void OnToggleCursor(InputValue v) { if (v.isPressed) LockCursor(Cursor.lockState != CursorLockMode.Locked); }

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

    void ActivateCamera() { if (cameraTransform) cameraTransform.gameObject.SetActive(true); }
    void DeactivateCamera() { if (cameraTransform) cameraTransform.gameObject.SetActive(false); }
    void LockCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
}
