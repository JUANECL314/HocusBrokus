using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    [Header("Flight Settings")]
    public bool enableFlying = true;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;

    [Tooltip("Umbral mínimo de input para considerar que hay movimiento (evita drift).")]
    [Range(0f, 1f)] public float axisDeadzone = 0.15f;

    [Tooltip("Usar GetAxisRaw (sin suavizado) para evitar micro-valores.")]
    public bool useRawAxes = true;

    [Header("Character Model")]
    public Transform characterModel; // Asigna tu modelo aquí

    private float yaw, pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    void Update()
    {
        if (!enableFlying) return;

        // Mouse look
        yaw += lookSpeed * Input.GetAxis("Mouse X");
        pitch -= lookSpeed * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        transform.eulerAngles = new Vector3(pitch, yaw, 0f);

        // Mantener el modelo vertical
        if (characterModel != null)
            characterModel.localEulerAngles = Vector3.zero;

        // --- Lectura de ejes con deadzone ---
        float h = useRawAxes ? Input.GetAxisRaw("Horizontal") : Input.GetAxis("Horizontal");
        float v = useRawAxes ? Input.GetAxisRaw("Vertical") : Input.GetAxis("Vertical");

        // Aplicar deadzone
        if (Mathf.Abs(h) < axisDeadzone) h = 0f;
        if (Mathf.Abs(v) < axisDeadzone) v = 0f;

        // Solo mover si hay input real
        if (h != 0f || v != 0f)
        {
            Vector3 move = new Vector3(h, 0f, v);
            transform.Translate(move * moveSpeed * Time.deltaTime, Space.Self);
        }

        // Subir/Bajar (teclas discretas, no necesitan deadzone)
        float verticalMove = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
        if (verticalMove != 0f)
        {
            transform.Translate(Vector3.up * verticalMove * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}