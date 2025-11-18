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
        if (fireVFX == null)
            fireVFX = transform.Find("VFX_TorchLight_2")?.gameObject;

        if (fireVFX != null)
            fireVFX.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only the master client triggers the burn logic 
        if (!PhotonNetwork.IsMasterClient) return;

        if (!isBurning && collision.collider.CompareTag("Fire"))
        {
            photonView.RPC("RPC_StartBurning", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_StartBurning()
    {
        if (isBurning) return;
        isBurning = true;

        if (fireVFX != null)
            fireVFX.SetActive(true);

        // Master destroys after X seconds
        if (PhotonNetwork.IsMasterClient)
            Invoke(nameof(NetworkDestroy), burnDuration);
    }

    void NetworkDestroy()
    {
        // Destroy the networked object on all clients
        PhotonNetwork.Destroy(gameObject);
    }
}
