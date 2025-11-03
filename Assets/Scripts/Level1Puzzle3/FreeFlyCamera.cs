using UnityEngine;
using UnityEngine.InputSystem;

public class FreeFlyDebug : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Look Settings")]
    public float lookSensitivity = 1.5f;
    public float maxHeadTilt = 80f;
    public bool invertY = false;

    [Header("References")]
    public Transform characterModel;
    public Transform cameraTransform;
    public Vector3 cameraOffset = Vector3.zero;

    private float yaw, pitch;
    private Vector2 moveInput;
    private float upDownInput;
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
        // --- ROTACIÓN ---
        float lookX = lookInput.x * lookSensitivity;
        float lookY = lookInput.y * lookSensitivity * (invertY ? 1f : -1f);

        yaw += lookX;
        pitch += lookY;
        pitch = Mathf.Clamp(pitch, -maxHeadTilt, maxHeadTilt);

        characterModel.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        cameraTransform.localPosition = cameraOffset;

        // --- MOVIMIENTO ---
        Vector3 move = characterModel.forward * moveInput.y +
                       characterModel.right * moveInput.x +
                       Vector3.up * upDownInput;

        transform.position += move * moveSpeed * Time.deltaTime;
    }

    // --- Input Callbacks (PlayerInput: Send Messages) ---

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnUpDown(InputValue value)
    {
        upDownInput = Mathf.Clamp(value.Get<float>(), -1f, 1f);
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
