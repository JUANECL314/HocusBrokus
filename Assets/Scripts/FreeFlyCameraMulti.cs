using UnityEngine;
using Photon.Pun;

public class FreeFlyCameraMulti : MonoBehaviourPun
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
    public Vector3 cameraOffset = Vector3.zero;
    public float maxHeadTilt = 80f;

    private bool isLocalMode; // ← indica si se está jugando sin Photon

    void Start()
    {
        // Detecta si Photon está conectado
        isLocalMode = !PhotonNetwork.IsConnected;

        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>(true)?.transform;

        if (characterModel == null)
            characterModel = transform;

        yaw = characterModel.eulerAngles.y;
        pitch = cameraTransform.localEulerAngles.x;

        // --- Control de cámara según modo ---
        if (isLocalMode)
        {
            Debug.Log("Modo local activado: control libre de cámara y movimiento");
            ActivarCamara();
        }
        else
        {
            // En red, solo el jugador dueño activa su cámara
            if (photonView.IsMine)
                ActivarCamara();
            else
                DesactivarCamara();
        }

        // Cursor visible y libre
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // --- ROTACIÓN DE CÁMARA ---
        yaw += Input.GetAxis("Mouse X") * lookSpeed;
        pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
        pitch = Mathf.Clamp(pitch, -maxHeadTilt, maxHeadTilt);

        characterModel.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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

    private void ActivarCamara()
    {
        if (cameraTransform != null)
            cameraTransform.gameObject.SetActive(true);
    }

    private void DesactivarCamara()
    {
        if (cameraTransform != null)
            cameraTransform.gameObject.SetActive(false);
    }
}
