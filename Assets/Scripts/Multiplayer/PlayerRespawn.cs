using Photon.Pun;
using UnityEngine;

public class PlayerRespawn : MonoBehaviourPun
{
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Trigger-based death (if Death has "Is Trigger" enabled)
    void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;
        if (other.CompareTag("Death"))
        {
            TeleportToSpawn();
        }
    }

    // Collision-based death (if Death is not a trigger)
    void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine) return;
        if (collision.collider.CompareTag("Death"))
        {
            TeleportToSpawn();
        }
    }

    void TeleportToSpawn()
    {
        Transform spawn = SpawnPointLobby.GetSpawnForActor(PhotonNetwork.LocalPlayer.ActorNumber);
        if (spawn == null)
        {
            Debug.LogWarning("No spawn point found for actor " + PhotonNetwork.LocalPlayer.ActorNumber);
            return;
        }
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.position = spawn.position;
        transform.rotation = spawn.rotation;
    }
}