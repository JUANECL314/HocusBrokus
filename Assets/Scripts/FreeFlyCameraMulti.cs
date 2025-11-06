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
    public float jumpForce = 5f;
    public LayerMask groundMask = ~0;
    public float groundCheckDistance = 0.2f;

    [Header("Movement (Free-Fly Debug)")]
    public float flySpeed = 8f;

    private Animator _animator;
    [Header("Look")]
    public float mouseSensitivity = 1.5f;
    public float gamepadSensitivity = 0.7f; // menor para stick
    public float maxHeadTilt = 80f;
    public bool invertY = false;

    [Header("References")]
    public Transform characterModel;  // yaw (cuerpo)
    public Transform cameraTransform; // pitch (cámara)
    public Vector3 cameraOffset = Vector3.zero;

    // state
    private Rigidbody rb;
    private float yaw, pitch;
    private Vector2 moveInput;   // Input Actions: Move (Vector2)
    private Vector2 lookInput;   // Input Actions: Look (Vector2)
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

        // Settings iniciales y suscripción
        
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>(true)?.transform;

        if (characterModel == null)
            characterModel = transform;

        // try to find Animator first on the characterModel, then in its children
        _animator = characterModel.GetComponent<Animator>();
        if (_animator == null)
            _animator = characterModel.GetComponentInChildren<Animator>(true);
            
        Debug.Log("characterModel: " + (characterModel ? characterModel.name : "null") +
                  ", Animator: " + (_animator != null ? "found on " + _animator.gameObject.name : "null"));
        yaw = characterModel.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;

        // --- Control de cámara según modo ---
        if (SettingsManager.I)
        {
            mouseSensitivity = SettingsManager.I.MouseSensitivity;
            gamepadSensitivity = SettingsManager.I.GamepadSensitivity;
            invertY = SettingsManager.I.InvertY;
            SettingsManager.I.OnChanged += ApplySettings;
        }

        // Solo mi player controla y muestra su cámara
        if (isLocalMode || photonView.IsMine) ActivateCamera();
        else
        {
            DeactivateCamera();
            // (Opcional) Deshabilitar el script completo si no es mío:
            // enabled = false; return;
        }

        // Rigidbody settings para movimiento normal
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        yaw = characterModel.eulerAngles.y;
        pitch = cameraTransform ? cameraTransform.localEulerAngles.x : 0f;

        LockCursor(true);
        if (cameraTransform) cameraTransform.localPosition = cameraOffset;
    }

    void Update()
    {
        if (!isLocalMode && !photonView.IsMine) return;

        // Toggle de modo debug (F1)
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            debugFreeFly = !debugFreeFly;

        // Sensibilidad según fuente (mouse vs stick)
        bool usingGamepad = Gamepad.current != null && Gamepad.current.rightStick.IsActuated();
        float sens = usingGamepad ? gamepadSensitivity : mouseSensitivity;

        // Limpieza de ruido mínimo en look
        if (lookInput.sqrMagnitude < 0.0005f) lookInput = Vector2.zero;

        // ROTACIÓN
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


        // UP/DOWN solo en modo free-fly (E/Q)
        if (debugFreeFly)
        {
            float upDown = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.eKey.isPressed) upDown += 1f;
                if (kb.qKey.isPressed) upDown -= 1f;
            }

            // Movimiento directo sin inercia
            Vector3 desired =
                characterModel.forward * moveInput.y +
                characterModel.right * moveInput.x +
                Vector3.up * upDown;

            if (desired.sqrMagnitude < 0.001f) desired = Vector3.zero;
            if (desired.sqrMagnitude > 1f) desired.Normalize();

            // Desacoplar de física mientras vuelas
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero; // <<< corregido
            transform.position += desired * flySpeed * Time.deltaTime;
        }
        else
        {
            // Volvemos a física normal
            if (!rb.useGravity) rb.useGravity = true;
        }
        // --- MOVIMIENTO CON WASD ---
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // use raw move (not normalized) to keep magnitude proportional to input
        Vector3 rawMove = characterModel.forward * vertical + characterModel.right * horizontal;
        float inputMagnitude = Mathf.Clamp01(new Vector2(horizontal, vertical).magnitude); // 0..1

        Vector3 move = rawMove.normalized; // direction for movement
        // apply _animator speed using inputMagnitude (matches joystick/WASD amount)
        if (_animator != null)
        {
            // ensure the parameter name "Speed" exactly matches your Animator parameter
            _animator.SetFloat("Speed", inputMagnitude, 0.1f, Time.deltaTime);
            Debug.Log("Set Animator 'Speed' = " + inputMagnitude);
        }
        else
        {
            Debug.Log("Animator is null, can't set Speed");
        }

        //transform.position += move * walkSpeed * inputMagnitude * Time.deltaTime;

    }

    void FixedUpdate()
    {
        if (!isLocalMode && !photonView.IsMine) return;
        if (debugFreeFly) return; // el free-fly ya se mueve en Update

        // MOVIMIENTO NORMAL con física (plano XZ)
        Vector3 wishDir = (characterModel.forward * moveInput.y) + (characterModel.right * moveInput.x);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        // Conserva la Y de la física (gravedad / saltos), controla XZ
        Vector3 vel = rb.linearVelocity; // <<< corregido
        Vector3 targetXZ = wishDir * walkSpeed;
        rb.linearVelocity = new Vector3(targetXZ.x, vel.y, targetXZ.z); // <<< corregido

        // Salto
        if (jumpPressed && IsGrounded())
        {
            // reset Y para salto consistente
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // <<< corregido
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
        jumpPressed = false; // consumir
    }

    bool IsGrounded()
    {
        // chequeo simple al centro del capsule/collider
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
        if (SettingsManager.I) SettingsManager.I.OnChanged -= ApplySettings; // <<< importante
    }

    // ===== Helpers =====
    void ActivateCamera() { if (cameraTransform) cameraTransform.gameObject.SetActive(true); }
    void DeactivateCamera() { if (cameraTransform) cameraTransform.gameObject.SetActive(false); }
    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
