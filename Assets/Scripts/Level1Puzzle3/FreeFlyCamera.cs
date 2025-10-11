using Photon.Pun;
using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;
    private float yaw, pitch;
    PhotonView vista;
    void Start()
    {
        vista = GetComponent<PhotonView>();
        // Confine the cursor inside the game window
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true; // keep it visible while testing
    }


    void Update()
    {
        if (vista.IsMine)
        {
            // Mouse look
            yaw += lookSpeed * Input.GetAxis("Mouse X");
            pitch -= lookSpeed * Input.GetAxis("Mouse Y");
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.eulerAngles = new Vector3(pitch, yaw, 0);

            // Movement
            Vector3 move = new Vector3(
                Input.GetAxis("Horizontal"),
                (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0),
                Input.GetAxis("Vertical")
            );
            transform.Translate(move * moveSpeed * Time.deltaTime);
        }
    }
}
