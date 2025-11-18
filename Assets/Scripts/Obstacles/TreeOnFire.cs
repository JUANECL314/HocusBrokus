using UnityEngine;
using Photon.Pun;

public class TreeOnFire : MonoBehaviourPun
{
    [Header("Reference to fire particle child")]
    public GameObject fireVFX;

    [Header("Settings")]
    public float burnDuration = 3.5f;

    private bool isBurning = false;

    void Start()
    {
        Debug.Log($"[TreeOnFire] Start on {gameObject.name}");

        if (fireVFX == null)
        {
            fireVFX = transform.Find("VFX_TorchLight_2")?.gameObject;
            Debug.Log($"[TreeOnFire] Auto-found fireVFX: {fireVFX}");
        }

        if (fireVFX != null)
        {
            fireVFX.SetActive(false);
            Debug.Log("[TreeOnFire] Fire VFX set to inactive.");
        }
        else
        {
            Debug.LogWarning("[TreeOnFire] WARNING: fireVFX is NULL!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[TreeOnFire] Trigger entered by: {other.name}, Tag: {other.tag}");

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[TreeOnFire] Not master client. Ignoring collision.");
            return;
        }

        if (!isBurning && other.CompareTag("Fire"))
        {
            Debug.Log("[TreeOnFire] Fire detected! Calling RPC_StartBurning...");
            photonView.RPC("RPC_StartBurning", RpcTarget.All);
        }
        else
        {
            Debug.Log("[TreeOnFire] Collision happened but did NOT match burning conditions.");
        }
    }

    [PunRPC]
    void RPC_StartBurning()
    {
        Debug.Log("[TreeOnFire] RPC_StartBurning called.");

        if (isBurning)
        {
            Debug.Log("[TreeOnFire] Already burning. Ignoring RPC.");
            return;
        }

        isBurning = true;

        if (fireVFX != null)
        {
            fireVFX.SetActive(true);
            Debug.Log("[TreeOnFire] Fire VFX ACTIVATED.");
        }
        else
        {
            Debug.LogWarning("[TreeOnFire] Cannot turn on fire VFX (NULL).");
        }

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[TreeOnFire] Scheduling destruction in {burnDuration} seconds.");
            Invoke(nameof(NetworkDestroy), burnDuration);
        }
    }

    void NetworkDestroy()
    {
        Debug.Log("[TreeOnFire] Destroying tree via PhotonNetwork.Destroy()");
        PhotonNetwork.Destroy(gameObject);
    }
}
