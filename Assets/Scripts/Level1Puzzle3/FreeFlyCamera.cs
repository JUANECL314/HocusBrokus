using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    [Header("Flight Settings")]
    public bool enableFlying = true;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float lookSpeed = 2f;

    [Header("References")]
    public Transform characterModel;   // El modelo o cuerpo del jugador
    public Transform cameraTransform;  // La cámara del jugador

    private float yaw, pitch;

    [Header("Camera Offset Limits")]
    public Vector3 cameraOffset = new Vector3(0f, 0f, 0f);  // posición fija dentro del jugador
    public float maxHeadTilt = 80f;  // límite vertical de rotación

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>().transform;

        if (characterModel == null)
            characterModel = transform;

        yaw = characterModel.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;

        // Mostrar el cursor y dejarlo libre
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
       // if (!enableFlying) return;

        // --- ROTACIÓN DE CÁMARA ---
        yaw += Input.GetAxis("Mouse X") * lookSpeed;
        pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
        pitch = Mathf.Clamp(pitch, -maxHeadTilt, maxHeadTilt);

        // Aplicar rotación al cuerpo (horizontal) y cámara (vertical)
        characterModel.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Mantener la cámara "pegada" al jugador
        cameraTransform.localPosition = cameraOffset;

        // --- MOVIMIENTO CON WASD ---
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = (characterModel.forward * vertical + characterModel.right * horizontal).normalized;
        transform.position += move * moveSpeed * Time.deltaTime;

        // --- MOVIMIENTO VERTICAL (E / Q) ---
        float verticalMove = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0);
        transform.position += Vector3.up * verticalMove * moveSpeed * Time.deltaTime;
    }
}
