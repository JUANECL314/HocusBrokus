using Photon.Pun;
using UnityEngine;
public class ButtonEnabler : MonoBehaviour, IObserver
{
    public ButtonActivation activatorButton;
    public ButtonActivation targetButton;

    void Start()
    {
        if (activatorButton != null)
        {
            activatorButton.AddObserver(this);
            Debug.Log("[ButtonEnabler] Suscrito al activatorButton");
        }
        else
        {
            Debug.LogError("[ButtonEnabler] ActivatorButton no asignado en el Inspector!");
        }

        if (targetButton == null)
        {
            Debug.LogError("[ButtonEnabler] TargetButton no asignado en el Inspector!");
        }
        else
        {
            targetButton.GetComponent<Renderer>().material.color = Color.gray;
        }
    }

    public void OnNotify(int id, bool state)
    {
        Debug.Log($"[ButtonEnabler] Recibido evento del botón {id} - State: {state}");

        if (state && targetButton != null)
        {
            Debug.Log("[ButtonEnabler] Activando targetButton");
            targetButton.SetEnabled(true);
            
            
            if (targetButton.photonView != null && targetButton.photonView.IsMine)
            {
                targetButton.photonView.RPC("RPC_SetEnabledAll", RpcTarget.All, true);
                activatorButton.SetPressed(true);
            }
        }
    }
}