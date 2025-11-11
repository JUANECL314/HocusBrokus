using UnityEngine;
using UnityEngine.InputSystem;
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
    public float gamepadSensitivity = 0.7f;
    public float maxHeadTilt = 80f;
    public bool invertY = false;

    [Header("References")]
    public Transform characterModel;  
    public Transform cameraTransform;
    public Vector3 cameraOffset = Vector3.zero;

    private Animator _animator;
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

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!cameraTransform)
            cameraTransform = GetComponentInChildren<Camera>(true)?.transform;
        if (!characterModel) characterModel = transform;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        isLocalMode = !PhotonNetwork.IsConnected;

        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>(true)?.transform;

        if (characterModel == null)
            characterModel = transform;

        _animator = characterModel.GetComponent<Animator>();
        if (_animator == null)
            _animator = characterModel.GetComponentInChildren<Animator>(true);

        Debug.Log("characterModel: " + (characterModel ? characterModel.name : "null") +
                  ", Animator: " + (_animator != null ? "found on " + _animator.gameObject.name : "null"));

        yaw = characterModel.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;

        if (SettingsManager.I)
        {
            mouseSensitivity = SettingsManager.I.MouseSensitivity;
            gamepadSensitivity = SettingsManager.I.GamepadSensitivity;
            invertY = SettingsManager.I.InvertY;
            SettingsManager.I.OnChanged += ApplySettings;
        }

        if (isLocalMode || photonView.IsMine) ActivateCamera();
        else DeactivateCamera();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        LockCursor(true);
        if (cameraTransform) cameraTransform.localPosition = cameraOffset;
    }

    void Update()
    {
        if (!isLocalMode && !photonView.IsMine) return;
        if (isFrozen) return;

        // Toggle debug mode
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            debugFreeFly = !debugFreeFly;

        // Rotation input
        bool usingGamepad = Gamepad.current != null && Gamepad.current.rightStick.IsActuated();
        float sens = usingGamepad ? gamepadSensitivity : mouseSensitivity;

        if (lookInput.sqrMagnitude < 0.0005f) lookInput = Vector2.zero;
        float lookX = lookInput.x * sens;
        float lookY = lookInput.y * sens * (invertY ? 1f : -1f);

        yaw += lookX;
        pitch = Mathf.Clamp(pitch + lookY, -maxHeadTilt, maxHeadTilt);

        if (characterModel)
            characterModel.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraTransform)
        {
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            cameraTransform.localPosition = cameraOffset;
        }

        // Input
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

            Vector3 flyDir = (
                characterModel.forward * moveInput.y +
                characterModel.right * moveInput.x +
                Vector3.up * upDown
            ).normalized;

            transform.position += flyDir * flySpeed * Time.fixedDeltaTime;
            return;
        }

        rb.useGravity = true;

        Vector3 moveDir = (characterModel.forward * moveInput.y + characterModel.right * moveInput.x).normalized;
        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);
        Vector3 targetXZ = moveDir * walkSpeed * inputMagnitude;

        Vector3 currentVel = rb.linearVelocity;
        Vector3 newVel = new Vector3(targetXZ.x, currentVel.y, targetXZ.z);
        rb.linearVelocity = newVel;

        if (jumpPressed && IsGrounded())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            if (_animator) _animator.SetTrigger("Jump");
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
    public void OnJump(InputValue value) { if (value.isPressed) jumpPressed = true; }
    public void OnToggleCursor(InputValue value)
    {
        if (value.isPressed) LockCursor(Cursor.lockState != CursorLockMode.Locked);
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
