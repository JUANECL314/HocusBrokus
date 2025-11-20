using UnityEngine;
using Photon.Pun;

public class FieryGround : MonoBehaviourPun
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[FieryGround] Trigger hit by: " + other.name);

        if (other.CompareTag("Water"))
        {
            Debug.Log("[FieryGround] Water detected! Deactivating across network...");
            photonView.RPC("RPC_Deactivate", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void RPC_Deactivate()
    {
        Debug.Log("[FieryGround] Deactivated by RPC.");
        gameObject.SetActive(false);
    }
}
