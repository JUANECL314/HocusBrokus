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
    public float maxHeadTilt = 150f;
    public bool invertY = false;

    [Header("References")]
    public Transform characterModel;  // yaw (body)
    public Transform cameraTransform; // pitch (camera)
    public Vector3 cameraOffset = Vector3.zero;

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

        // Note: your original code used rb.velocity - keep the same variable usage here.
        // If Unity version uses Rigidbody.velocity, that will still work with AddForce below.
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

        if (frozen)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            LockCursor(true);
        }
    }

    // =========================
    // Multiplayer push API
    // =========================

    /// <summary>
    /// Apply an instantaneous force/impulse on the local (owner) client.
    /// Use this for local testing (singleplayer).
    /// </summary>
    public void ApplyForceLocal(Vector3 force)
    {
        if (!IsLocalOwner) return;
        if (rb == null) return;
        rb.AddForce(force, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Photon RPC: called on the owner client to apply a push/impulse.
    /// Call from master/client: playerPhotonView.RPC("RPC_ApplyForce", playerPhotonView.Owner, forceVector);
    /// </summary>
    [PunRPC]
    public void RPC_ApplyForce(Vector3 force)
    {
        // This RPC will be executed on the target client's instance of this PhotonView.
        // Apply the impulse locally (instantaneous velocity change).
        if (rb == null) return;
        rb.AddForce(force, ForceMode.VelocityChange);
    }
}
