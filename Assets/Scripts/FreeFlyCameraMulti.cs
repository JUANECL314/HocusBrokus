using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // ⬅️ NUEVO
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class FreeFlyCameraMulti : MonoBehaviourPun
{
    [Header("Mode")]
    public bool debugFreeFly = false;

    [Header("Movement (Normal)")]
    public float walkSpeed = 5f;
    [Tooltip("Jump force actually used at runtime (puede ser override por escena).")]
    public float jumpForce = 20f;
    public LayerMask groundMask = ~0;
    public float groundCheckDistance = 0.2f;

    [Header("Movement (Free-Fly Debug)")]
    public float flySpeed = 8f;

    [Header("Look")]
    public float mouseSensitivity = 1.5f;
    public float gamepadSensitivity = 0.7f;
    public float maxHeadTilt = 150f;
    public bool invertY = false;

    [Header("References")]
    public Transform characterModel;  // yaw (body)
    public Transform cameraTransform; // pitch (camera)
    public Vector3 cameraOffset = Vector3.zero;

    // === NUEVO: configuración de salto por escena ===
    [System.Serializable]
    public class SceneJumpConfig
    {
        public string sceneName;   // nombre exacto de la escena
        public float jumpForce;    // fuerza de salto para esa escena
    }

    [Header("Jump per Scene")]
    [Tooltip("Si está activo, buscará la fuerza de salto según el nombre de la escena actual.")]
    public bool useSceneJumpConfig = true;
    [Tooltip("Valor por defecto si la escena no está en la lista.")]
    public float defaultJumpForce = 20f;
    public SceneJumpConfig[] sceneJumpConfigs;

    // Animator (synchronized by PhotonAnimatorView)
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

    // convenience
    public bool IsLocalOwner => isLocalMode || photonView.IsMine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!cameraTransform)
            cameraTransform = GetComponentInChildren<Camera>(true)?.transform;
        if (!characterModel)
            characterModel = transform;
        // animator lives in the character model
        _animator = characterModel ? characterModel.GetComponentInChildren<Animator>(true) : null;
    }

    void Start()
    {
        // Rigidbody setup
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
        // Camera only active for the owner (or in local mode)
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

        // === NUEVO: aplicar fuerza de salto según la escena ===
        ApplySceneJumpForce();
    }

    /// <summary>
    /// Ajusta jumpForce según la escena actual usando la lista sceneJumpConfigs.
    /// </summary>
    void ApplySceneJumpForce()
    {
        if (!useSceneJumpConfig)
        {
            // si no quieres usarlo, respeta el valor actual
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;

        // valor por defecto primero
        jumpForce = defaultJumpForce;

        if (sceneJumpConfigs == null || sceneJumpConfigs.Length == 0)
            return;

        foreach (var cfg in sceneJumpConfigs)
        {
            if (!string.IsNullOrEmpty(cfg.sceneName) && cfg.sceneName == currentScene)
            {
                jumpForce = cfg.jumpForce;
                // Debug opcional para validar
                Debug.Log($"[FreeFlyCameraMulti] Escena '{currentScene}' usando jumpForce = {jumpForce}");
                return;
            }
        }

        // Debug opcional para cuando no encuentra la escena
        Debug.Log($"[FreeFlyCameraMulti] Escena '{currentScene}' no encontrada en sceneJumpConfigs, usando defaultJumpForce = {defaultJumpForce}");
    }

    void Update()
    {
        if (!isLocalMode && !photonView.IsMine) return;
        if (isFrozen) return;

        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            debugFreeFly = !debugFreeFly;

        // LOOK
        bool usingGamepad = Gamepad.current != null && Gamepad.current.rightStick.IsActuated();
        float sens = usingGamepad ? gamepadSensitivity : mouseSensitivity;

        if (lookInput.sqrMagnitude < 0.0005f) lookInput = Vector2.zero;

        float lookX = lookInput.x * sens;
        float lookY = lookInput.y * sens * (invertY ? 1f : -1f);

        // camera pitch
        pitch = Mathf.Clamp(pitch + lookY, -maxHeadTilt, maxHeadTilt);

        // yaw
        yaw += lookX;

        // Apply rotation to the body
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Apply rotation to the camera
        if (cameraTransform)
        {
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            cameraTransform.localPosition = cameraOffset;
        }

        // Eye offset calculation (local to body)
        float maxLookAngle = 25f;   // how far eyes/head can look relative to body
        float deadzone = 15f;       // circular radius for eyes-only movement
        float reCenterSpeed = 5f;   // how fast eyes recenters

        float localYawDelta = Mathf.DeltaAngle(transform.eulerAngles.y, cameraTransform.eulerAngles.y);
        float clampedYaw = Mathf.Clamp(localYawDelta, -maxLookAngle, maxLookAngle);

        if (Mathf.Abs(clampedYaw) > deadzone)
            clampedYaw = Mathf.Lerp(clampedYaw, 0f, Time.deltaTime * reCenterSpeed);

        eyeLookOffset = new Vector2(clampedYaw / maxLookAngle, lookY / maxHeadTilt);

        // Movement input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
            jumpPressed = true;

        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);
        if (_animator)
            _animator.SetFloat("Speed", inputMagnitude, 0.1f, Time.deltaTime);
        HandleWalkSFX();
    }

    void FixedUpdate()
    {
        if (!isLocalMode && !photonView.IsMine) return;

        if (debugFreeFly) return; // free-fly handled in Update when enabled
        if (isFrozen) return;

        // MOVEMENT (XZ plane)
        Vector3 wishDir = (characterModel.forward * moveInput.y) + (characterModel.right * moveInput.x);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        Vector3 vel = rb.linearVelocity;
        Vector3 targetXZ = wishDir * walkSpeed;
        rb.linearVelocity = new Vector3(targetXZ.x, vel.y, targetXZ.z);

        // Jump
        if (jumpPressed && IsGrounded())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

            if (_animator) _animator.SetTrigger(PARAM_JUMP);
            SoundManager.Instance.Play(SfxKey.Jump, transform);
        }

        // Animation speed
        if (_animator)
        {
            float speed = Mathf.Clamp01(moveInput.magnitude);
            _animator.SetFloat(PARAM_SPEED, speed, 0.1f, Time.deltaTime);
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

    private bool isWalkingSfxPlaying = false;

    void HandleWalkSFX()
    {
        if (_animator == null) return;

        bool isMoving = _animator.GetFloat("Speed") > 0.1f;
        bool grounded = IsGrounded();

        if (isMoving && grounded)
        {
            if (!isWalkingSfxPlaying)
            {
                isWalkingSfxPlaying = true;
                SoundManager.Instance.StartLoop("playerWalk_" + photonView.ViewID, SfxKey.Walk, transform);
            }
        }
        else
        {
            if (isWalkingSfxPlaying)
            {
                isWalkingSfxPlaying = false;
                SoundManager.Instance.StopLoop("playerWalk_" + photonView.ViewID);
            }
        }
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

        moveInput = Vector2.zero;

        // forzar anim a idle
        if (_animator)
        {
            // Speed = 0 → estado Idle en tu blend tree
            _animator.SetFloat(PARAM_SPEED, 0f);

            // si fuera bool usa SetBool en lugar de ResetTrigger)
            _animator.ResetTrigger(PARAM_JUMP);
        }

        if (rb != null && frozen)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (frozen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            LockCursor(true);
        }
    }



    public void ApplyForceLocal(Vector3 force)
    {
        if (!IsLocalOwner) return;
        if (rb == null) return;
        rb.AddForce(force, ForceMode.VelocityChange);
    }

    [PunRPC]
    public void RPC_ApplyForce(Vector3 force)
    {
        if (rb == null) return;
        rb.AddForce(force, ForceMode.VelocityChange);
    }
}
