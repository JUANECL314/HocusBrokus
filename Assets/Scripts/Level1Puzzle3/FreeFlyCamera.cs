using UnityEngine;
using UnityEngine.InputSystem;

public class FreeFlyDebug : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Look Settings")]
    public float mouseSensitivity = 1.5f;
    public float gamepadSensitivity = 0.7f; // menos sensible para stick
    public float maxHeadTilt = 80f;
    public bool invertY = false;

    [Header("References")]
    public Transform characterModel;
    public Transform cameraTransform;
    public Vector3 cameraOffset = Vector3.zero;

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
        pitch = cameraTransform.localEulerAngles.x;
    }

    void Start()
    {
        LockCursor(true);
        cameraTransform.localPosition = cameraOffset;
    }

    void Update()
    {
        // Detectar si la entrada de look viene del gamepad
        bool usingGamepad = Gamepad.current != null && Gamepad.current.rightStick.IsActuated();

        float sensitivity = usingGamepad ? gamepadSensitivity : mouseSensitivity;

        // --- LOOK ---
        // limpieza de ruido mínimo
        if (lookInput.sqrMagnitude < 0.0005f)
            lookInput = Vector2.zero;

        float lookX = lookInput.x * sensitivity;
        float lookY = lookInput.y * sensitivity * (invertY ? 1f : -1f);

        yaw += lookX;
        pitch += lookY;
        pitch = Mathf.Clamp(pitch, -maxHeadTilt, maxHeadTilt);

        characterModel.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        cameraTransform.localPosition = cameraOffset;

        // --- UP/DOWN por teclado (E/Q) ---
        float upDownInput = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.eKey.isPressed) upDownInput += 1f;
            if (kb.qKey.isPressed) upDownInput -= 1f;
        }

        // --- MOVE ---
        Vector3 desired =
            characterModel.forward * moveInput.y +
            characterModel.right * moveInput.x +
            Vector3.up * upDownInput;

        // limpieza de ruido mínimo (drift)
        if (desired.sqrMagnitude < 0.001f)
            desired = Vector3.zero;

        if (desired.sqrMagnitude > 1f)
            desired.Normalize();

        transform.position += desired * moveSpeed * Time.deltaTime;
    }

    // --- Input Callbacks ---

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnToggleCursor(InputValue value)
    {
        if (value.isPressed)
            LockCursor(Cursor.lockState != CursorLockMode.Locked);
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
