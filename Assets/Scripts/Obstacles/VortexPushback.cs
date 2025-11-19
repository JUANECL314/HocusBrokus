using UnityEngine;
using Photon.Pun;

public class VortexObstacle : MonoBehaviourPun
{
    [Header("Push Settings")]
    public float pushForce = 18f;
    public float pushUpForce = 8f;
    public float pushHorizontalForce = -10f; // negative = left
    public float pushDuration = 0.35f;

    [Header("Debug")]
    public bool debugLogs = true;

    private void Start()
    {
        if (debugLogs) Debug.Log("[VortexObstacle] Initialized.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (debugLogs) Debug.Log("[VortexObstacle] Trigger Enter by: " + other.name);

        // -------------------------------------
        // 1. Wind Magic dissipates vortex
        // -------------------------------------
        if (other.CompareTag("Wind"))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (debugLogs) Debug.Log("[VortexObstacle] Wind detected → Deactivating vortex...");

                // Synchronize deactivation on all clients
                photonView.RPC("RPC_DeactivateVortex", RpcTarget.All);
            }
            return;
        }

        // -------------------------------------
        // 2. Player gets pushed back
        // -------------------------------------
        if (other.CompareTag("Player"))
        {
            // Only push *your own* player
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                if (debugLogs) Debug.Log("[VortexObstacle] Pushing local player...");

                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 pushDirection = 
                        (transform.forward * pushForce) +
                        (Vector3.up * pushUpForce) +
                        (transform.right * pushHorizontalForce);

                    rb.AddForce(pushDirection, ForceMode.VelocityChange); 
                }

            }
        }
    }

    // -------------------------------------------------------
    // RPC: Deactivate vortex on all clients
    // -------------------------------------------------------
    [PunRPC]
    void RPC_DeactivateVortex()
    {
        if (debugLogs) Debug.Log("[VortexObstacle] RPC_DeactivateVortex → Vortex disabled.");
        gameObject.SetActive(false);
    }
}
