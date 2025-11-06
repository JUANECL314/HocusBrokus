using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class FreeFlyCameraMulti : MonoBehaviourPun
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    private Animator animator;
    public float lookSpeed = 2f;

    [Header("References")]
    public Transform characterModel;
    public Transform cameraTransform;

    private float yaw, pitch;

    [Header("Camera Offset Limits")]
    public Vector3 cameraOffset = Vector3.zero;
    public float maxHeadTilt = 80f;

    private bool isLocalMode;

    [Header("Jump Settings")]
    public float jumpHeight = 10f;
    public float gravity = -9.81f;

    private float verticalVelocity = 0f;
    private bool hasJumped = false;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;          // prevent unwanted spinning
        rb.useGravity = true;              // let Unity handle gravity

        isLocalMode = !PhotonNetwork.IsConnected;

        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>(true)?.transform;

        if (characterModel == null)
            characterModel = transform;

        animator = characterModel.GetComponent<Animator>();
        if (animator == null)
            animator = characterModel.GetComponentInChildren<Animator>(true);

        Debug.Log("characterModel: " + (characterModel ? characterModel.name : "null") +
                  ", Animator: " + (animator != null ? "found on " + animator.gameObject.name : "null"));

        yaw = characterModel.eulerAngles.y;
        pitch = cameraTransform != null ? cameraTransform.localEulerAngles.x : 0f;

        if (isLocalMode)
            ActivarCamara();
        else
        {
            if (photonView.IsMine)
                ActivarCamara();
            else
                DesactivarCamara();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (!isLocalMode && !photonView.IsMine) return;

        // Rotation
        yaw += Input.GetAxis("Mouse X") * lookSpeed;
        pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
        pitch = Mathf.Clamp(pitch, -maxHeadTilt, maxHeadTilt);

        if (characterModel != null)
            characterModel.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            cameraTransform.localPosition = cameraOffset;
        }

        // Jump input
        if (Input.GetKeyDown(KeyCode.Space) && !hasJumped && IsGrounded())
        {
            float v = Mathf.Sqrt(Mathf.Max(0f, jumpHeight * -2f * gravity));
            verticalVelocity = v;
            rb.AddForce(Vector3.up * v, ForceMode.VelocityChange);
            hasJumped = true;
            if (animator != null) animator.SetTrigger("Jump");
            StartCoroutine(ResetJumpAfterDelay(0.2f));
        }

        // Movement input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 rawMove = characterModel.forward * vertical + characterModel.right * horizontal;
        float inputMagnitude = Mathf.Clamp01(new Vector2(horizontal, vertical).magnitude);
        Vector3 moveDir = rawMove.normalized * moveSpeed * inputMagnitude;

        if (animator != null)
            animator.SetFloat("Speed", inputMagnitude, 0.1f, Time.deltaTime);

        // Apply movement with Rigidbody
        Vector3 newPosition = rb.position + moveDir * Time.deltaTime;
        rb.MovePosition(newPosition);
    }

    private System.Collections.IEnumerator ResetJumpAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        hasJumped = false;
    }

    private bool IsGrounded()
    {
        // Raycast slightly below the collider to detect the floor
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f);
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
