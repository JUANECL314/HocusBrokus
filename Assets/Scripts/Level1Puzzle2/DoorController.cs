using System;
using UnityEngine;
using Photon.Pun;
public class DoorController : MonoBehaviourPun, IObserver
{
    public ButtonActivation[] mainButtons; // Solo los botones principales
    private bool doorOpened = false;

    void Start()
    {
        foreach (var b in mainButtons)
            b.AddObserver(this);
    }

    public void OnNotify(int id, bool state)
    {
        if (!doorOpened && AllPressed())
        {
            photonView.RPC(nameof(RPC_OpenDoor), RpcTarget.AllBuffered);
        }
    }

    private bool AllPressed()
    {
        foreach (ButtonActivation b in mainButtons)
            if (!b.isEnabled || !b.isPressed)
                return false;
        return true;
    }

    [PunRPC]
    private void RPC_OpenDoor()
    {
        if (doorOpened) return;

        doorOpened = true;

        // Si hay animación, aquí iría
        // animator.SetTrigger("Open"); 

        // O desactivas el collider, o cambias el estado de la puerta:
        Debug.Log("Puerta abierta sincronizada por Photon!");
        gameObject.SetActive(false); // Desaparece la puerta al abrirse
    }
}