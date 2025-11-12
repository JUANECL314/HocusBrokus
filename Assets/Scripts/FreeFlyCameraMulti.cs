using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class FreeFlyCameraMulti : MonoBehaviourPun
{
    [Header("Mode")]
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
    public float gamepadSensitivity = 0.7f;
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
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool isLocalMode;
    private bool isFrozen = false;


    [Header("Jump Settings")]
    public float jumpHeight = 10f;
    public float gravity = -9.81f;

    private float verticalVelocity = 0f;
    private bool hasJumped = false;

    // 👁 For Eye Look System
    [HideInInspector] public Vector2 eyeLookOffset;
    private float currentLookAngle = 0f;

    // 👁 Optional getter for external access (e.g. iris)
    public Vector2 EyeLookOffset => eyeLookOffset;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!cameraTransform)
            cameraTransform = GetComponentInChildren<Camera>(true)?.transform;
        if (!characterModel)
            characterModel = transform;
        // animator vive en el modelo del personaje
        _animator = characterModel ? characterModel.GetComponentInChildren<Animator>(true) : null;
    }

    void Start()
    {
        rb.freezeRotation = true;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

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
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>(true)?.transform;

        if (characterModel == null)
            characterModel = transform;

        _animator = characterModel.GetComponent<Animator>();
        if (_animator == null)
            _animator = characterModel.GetComponentInChildren<Animator>(true);

        yaw = characterModel.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;



        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        LockCursor(true);
        if (cameraTransform)
            cameraTransform.localPosition = cameraOffset;
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

        // Apply camera pitch (up/down)
        pitch = Mathf.Clamp(pitch + lookY, -maxHeadTilt, maxHeadTilt);

        // Accumulate yaw freely for 360° rotation
        yaw += lookX;

        // Apply rotation to the body
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Apply rotation to the camera
        if (cameraTransform)
        {
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            cameraTransform.localPosition = cameraOffset;
        }

        // --- Eye Offset Calculation (local to the body) ---
        float maxLookAngle = 25f;   // how far eyes/head can look relative to body
        float deadzone = 15f;       // circular radius for eyes-only movement
        float reCenterSpeed = 5f;   // how fast eyes recenters

        // Calculate yaw difference between camera and body (handles 360° wrap)
        float localYawDelta = Mathf.DeltaAngle(transform.eulerAngles.y, cameraTransform.eulerAngles.y);
        float clampedYaw = Mathf.Clamp(localYawDelta, -maxLookAngle, maxLookAngle);

        // Smoothly recenters when player rotates
        if (Mathf.Abs(clampedYaw) > deadzone)
            clampedYaw = Mathf.Lerp(clampedYaw, 0f, Time.deltaTime * reCenterSpeed);

        // Store this for the iris/eye system
        eyeLookOffset = new Vector2(clampedYaw / maxLookAngle, lookY / maxHeadTilt);

        // Movement input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
            jumpPressed = true;

        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);
        if (_animator)
            _animator.SetFloat("Speed", inputMagnitude, 0.1f, Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (!isLocalMode && !photonView.IsMine) return;
        if (isFrozen) return;

        if (debugFreeFly)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;

            float upDown = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.eKey.isPressed) upDown += 1f;
                if (kb.qKey.isPressed) upDown -= 1f;
            }

            Vector3 desired =(
                characterModel.forward * moveInput.y +
                characterModel.right * moveInput.x +
                Vector3.up * upDown
            ).normalized;

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
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule == null) return false;

        float radius = capsule.radius * 0.95f;
        float castDistance = groundCheckDistance;
        Vector3 origin = transform.position + Vector3.up * (capsule.radius + 0.05f);

        return Physics.SphereCast(origin, radius, Vector3.down, out _, castDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
            jumpPressed = true;
    }

    public void OnToggleCursor(InputValue value)
    {
        if (value.isPressed)
            LockCursor(Cursor.lockState != CursorLockMode.Locked);
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
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
