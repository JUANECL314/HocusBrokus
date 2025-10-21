using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    [Header("Flight Settings")]
    public bool enableFlying = true;
    
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;
    
    [Header("Character Model")]
    public Transform characterModel; // Assign your character model here
    
    private float yaw, pitch;

    void Start()
    {
        // Confine the cursor inside the game window
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true; // keep it visible while testing
    }

    void Update()
    {
        if (!enableFlying) return;

        // Mouse look - rotate camera on both axes
        yaw += lookSpeed * Input.GetAxis("Mouse X");
        pitch -= lookSpeed * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        
        // Apply rotation to camera
        transform.eulerAngles = new Vector3(pitch, yaw, 0);
        
        // Keep character model upright by zeroing its local rotation
        if (characterModel != null)
        {
            characterModel.localEulerAngles = Vector3.zero;
        }

        // Horizontal movement (relative to camera rotation)
        Vector3 move = new Vector3(
            Input.GetAxis("Horizontal"),
            0,
            Input.GetAxis("Vertical")
        );
        transform.Translate(move * moveSpeed * Time.deltaTime, Space.Self);
        
        // Vertical movement (world space - no rotation)
        float verticalMove = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0);
        transform.Translate(Vector3.up * verticalMove * moveSpeed * Time.deltaTime, Space.World);
    }
}