using UnityEngine;
using UnityEngine.InputSystem; // Input System
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class FreeFlyCameraMulti : MonoBehaviourPun
{
    [Header("Mode")]
    [Tooltip("F1 alterna en runtime. Cuando está desactivado, usa movimiento normal con física.")]
    public bool debugFreeFly = false;

    [Header("Movement (Normal)")]
    public float walkSpeed = 5f;
    public float jumpForce = 20f;
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
    public Transform cameraTransform; // pitch (cámara)
    public Vector3 cameraOffset = Vector3.zero;

    // Animator (sincronizado por PhotonAnimatorView)
    private Animator _animator;
    private const string PARAM_SPEED = "Speed";
    private const string PARAM_GROUNDED = "Grounded";
    private const string PARAM_JUMP = "Jump";

    // state
    private Rigidbody rb;
    private float yaw, pitch;
    private Vector2 moveInput;   // Input Actions: Move (Vector2)
    private Vector2 lookInput;   // Input Actions: Look (Vector2)
    private bool jumpPressed;
    private bool isLocalMode;
    private bool isFrozen = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!cameraTransform) cameraTransform = GetComponentInChildren<Camera>(true)?.transform;
        if (!characterModel) characterModel = transform;

        // animator vive en el modelo del personaje
        _animator = characterModel ? characterModel.GetComponentInChildren<Animator>(true) : null;
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

        // Cámara solo del dueño
        if (isLocalMode || photonView.IsMine) ActivateCamera();
        else DeactivateCamera();

        // Rigidbody: bloquear rotaciones por física
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        yaw = characterModel.eulerAngles.y;
        pitch = cameraTransform ? cameraTransform.localEulerAngles.x : 0f;

        LockCursor(true);
        if (cameraTransform) cameraTransform.localPosition = cameraOffset;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (!isLocalMode && !photonView.IsMine) return;

        if (isFrozen) return;
        // Toggle de modo debug (F1)

        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            debugFreeFly = !debugFreeFly;

        // LOOK
        bool usingGamepad = Gamepad.current != null && Gamepad.current.rightStick.IsActuated();
        float sens = usingGamepad ? gamepadSensitivity : mouseSensitivity;

        if (lookInput.sqrMagnitude < 0.0005f) lookInput = Vector2.zero;

        float lookX = lookInput.x * sens;
        float lookY = lookInput.y * sens * (invertY ? 1f : -1f);

        yaw += lookX;
        pitch += lookY;
        pitch = Mathf.Clamp(pitch, -maxHeadTilt, maxHeadTilt);

        if (characterModel) characterModel.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraTransform)
        {
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            cameraTransform.localPosition = cameraOffset;
        }

        // FREE-FLY: mover en Update directamente (sin física)
        if (debugFreeFly)
        {
            float upDown = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.eKey.isPressed) upDown += 1f;
                if (kb.qKey.isPressed) upDown -= 1f;
            }

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

        // === ANIMACIÓN (solo dueño escribe; PhotonAnimatorView replica) ===
        if (_animator)
        {
            float speed = Mathf.Clamp01(moveInput.magnitude); // 0..1 desde Input System
            bool grounded = IsGrounded();
            _animator.SetFloat(PARAM_SPEED, speed, 0.1f, Time.deltaTime);
            _animator.SetBool(PARAM_GROUNDED, grounded);
        }
    }

    void FixedUpdate()
    {
        if (!isLocalMode && !photonView.IsMine) return;

        if (debugFreeFly) return; // el free-fly ya se mueve en Update
        if (isFrozen) return;
        // MOVIMIENTO NORMAL con física (plano XZ)
        Vector3 wishDir = (characterModel.forward * moveInput.y) + (characterModel.right * moveInput.x);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        Vector3 vel = rb.linearVelocity;
        Vector3 targetXZ = wishDir * walkSpeed;
        rb.linearVelocity = new Vector3(targetXZ.x, vel.y, targetXZ.z);

        // Salto (consumimos bandera)
        if (jumpPressed && IsGrounded())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

            if (_animator) _animator.SetTrigger(PARAM_JUMP); // dispara trigger para sync
        }
        jumpPressed = false;
    }

    bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.05f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.05f, groundMask, QueryTriggerInteraction.Ignore);
    }

    // ===== Input System (PlayerInput → Send Messages) =====
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
    public void OnJump(InputValue value) { if (value.isPressed) jumpPressed = true; }
    public void OnToggleCursor(InputValue value)
    {
        if (value.isPressed) LockCursor(Cursor.lockState != CursorLockMode.Locked);
    }

    // ===== Settings binding =====
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

    // ===== Helpers =====
    void ActivateCamera() { if (cameraTransform) cameraTransform.gameObject.SetActive(true); }
    void DeactivateCamera() { if (cameraTransform) cameraTransform.gameObject.SetActive(false); }
    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;

        if (frozen)
        {
            // Detener cualquier movimiento actual
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Liberar el cursor para usar UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Recuperar control normal del cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
