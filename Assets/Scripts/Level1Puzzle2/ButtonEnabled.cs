using Photon.Pun;
using UnityEngine;
public class ButtonEnabler : MonoBehaviour, IObserver
{
    public ButtonActivation activatorButton;
    public ButtonActivation targetButton;
    public GameObject puerta;
    public GameObject pointInteracive;
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
        if (puerta != null) puerta.SetActive(true);
        if (pointInteracive != null) pointInteracive.SetActive(false);
    }

    public void OnNotify(int id, bool state)
    {
        if (state && targetButton != null)
        {
            // Solo habilitamos el botón objetivo
            targetButton.SetEnabled(true);
            puerta.SetActive(false);
            pointInteracive.SetActive(true);
            // Enviar RPC para actualizar visual a todos los clientes
            if (targetButton.photonView != null && targetButton.photonView.IsMine)
            {
                targetButton.photonView.RPC("RPC_SetEnabledAll", RpcTarget.All, true);
            }

            
        }
    }
}