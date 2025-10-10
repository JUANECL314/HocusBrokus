using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class Control2 : MonoBehaviour
{
    public float velocidad = 10f;
    PhotonView vista;
    private void Start()
    {
        vista = GetComponent<PhotonView>();
    }
    void Movimiento()
    {
        // El nuevo sistema usa Keyboard.current
        float x = 0f;
        float z = 0f;

        if (Keyboard.current.wKey.isPressed) x += 1;
        if (Keyboard.current.sKey.isPressed) x -= 1;
        if (Keyboard.current.aKey.isPressed) z += 1;
        if (Keyboard.current.dKey.isPressed) z -= 1;

        Vector3 direccion = new Vector3(x, 0, z).normalized;
        transform.Translate(direccion * velocidad * Time.deltaTime);
    }

    void Update()
    {
        if (vista.IsMine)
        {
            Movimiento();
        }
    }
}