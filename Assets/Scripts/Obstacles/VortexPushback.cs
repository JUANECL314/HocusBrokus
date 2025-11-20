using UnityEngine;
using Photon.Pun;

public class VortexObstacle : MonoBehaviourPun
{
    [Header("Push Settings")]
    public float pushForce = 18f;
    public float pushUpForce = 8f;
    public float pushHorizontalForce = -10f; // negative = left
    public float pushDuration = 0.8f;

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
        // 1. Wind Magic dissipates vortex (keep existing behavior)
        // -------------------------------------
        if (other.CompareTag("Wind"))
        {
            Debug.Log("[VortexObstacle] Wind detected → Deactivating vortex...");
            photonView.RPC("RPC_DeactivateVortex", RpcTarget.AllBuffered);
            return;
        }

        // -------------------------------------
        // 2. Player gets pushed back (local owner only)
        // -------------------------------------
        if (other.CompareTag("Player"))
        {
            PhotonView pv = other.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                if (debugLogs) Debug.Log("[VortexObstacle] Starting vortex push on local player.");

                PlayerVortexReceiver receiver = other.GetComponent<PlayerVortexReceiver>();
                if (receiver != null)
                {
                    receiver.StartVortexPush(transform, pushDuration, pushForce, pushUpForce, pushHorizontalForce);
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
