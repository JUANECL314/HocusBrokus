using UnityEngine;
using Photon.Pun;

public class FieryGround : MonoBehaviourPun
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[FieryGround] Trigger hit by: " + other.name);

        // Only the master client is allowed to change networked objects
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[FieryGround] Not master client, ignoring.");
            return;
        }

        if (other.CompareTag("Water"))
        {
            Debug.Log("[FieryGround] Water detected! Deactivating fiery ground...");
            photonView.RPC("RPC_Deactivate", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_Deactivate()
    {
        Debug.Log("[FieryGround] Deactivated by RPC.");
        gameObject.SetActive(false);
    }
}
