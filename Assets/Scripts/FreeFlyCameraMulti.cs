using UnityEngine;
using Photon.Pun;

public class FreeFlyCameraMulti : MonoBehaviourPun
{

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    private Animator animator;
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

        // try to find Animator first on the characterModel, then in its children
        animator = characterModel.GetComponent<Animator>();
        if (animator == null)
            animator = characterModel.GetComponentInChildren<Animator>(true);
            
        Debug.Log("characterModel: " + (characterModel ? characterModel.name : "null") +
                  ", Animator: " + (animator != null ? "found on " + animator.gameObject.name : "null"));
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

// ...existing code...
        // --- MOVIMIENTO CON WASD ---
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // use raw move (not normalized) to keep magnitude proportional to input
        Vector3 rawMove = characterModel.forward * vertical + characterModel.right * horizontal;
        float inputMagnitude = Mathf.Clamp01(new Vector2(horizontal, vertical).magnitude); // 0..1

        Vector3 move = rawMove.normalized; // direction for movement
        // apply animator speed using inputMagnitude (matches joystick/WASD amount)
        if (animator != null)
        {
            // ensure the parameter name "Speed" exactly matches your Animator parameter
            animator.SetFloat("Speed", inputMagnitude, 0.1f, Time.deltaTime);
            Debug.Log("Set Animator 'Speed' = " + inputMagnitude);
        }
        else
        {
            Debug.Log("Animator is null, can't set Speed");
        }

        transform.position += move * moveSpeed * inputMagnitude * Time.deltaTime;
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
