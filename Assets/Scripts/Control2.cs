using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class Control2 : MonoBehaviour
{
    public float speed = 10.0f;
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;
    private float yaw, pitch;
    public float velocidad = 10f;
    public float rotationSpeed = 100.0f;
    PhotonView vista;

    
        
    private void Start()
    {
        vista = GetComponent<PhotonView>();
    }
    void Movimiento()
    {
        // Mouse look
        /*yaw += lookSpeed * Input.GetAxis("Mouse X");
        pitch -= lookSpeed * Input.GetAxis("Mouse Y");
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        transform.eulerAngles = new Vector3(pitch, yaw, 0);*/
        // El nuevo sistema usa Keyboard.current
        float translation = Input.GetAxis("Vertical") * speed;
        float rotation = Input.GetAxis("Horizontal") * rotationSpeed;

        // Make it move 10 meters per second instead of 10 meters per frame...
        translation *= Time.deltaTime;
        rotation *= Time.deltaTime;

        // Move translation along the object's z-axis
        transform.Translate(0, 0, translation);

        // Rotate around our y-axis
        transform.Rotate(0, rotation, 0);
    }

    void Update()
    {
        
        if (vista.IsMine)
        {
            Movimiento();
        }
    }
}