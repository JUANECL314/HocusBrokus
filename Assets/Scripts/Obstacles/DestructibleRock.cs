using UnityEngine;
using Photon.Pun;

public class DestructibleRock : MonoBehaviourPun
{
    [Header("Rock Parts")]
    public GameObject firstBreakPart;       // large initial piece
    public GameObject[] remainingParts;     // other 3 parts or any number of parts

    [Header("Settings")]
    public int hitsNeeded = 2;              // 1st hit removes big piece, 2nd hit clears rest
    public float destroyDelay = 0.5f;

    private int hitCount = 0;

    void Start()
    {
        Debug.Log("[DestructibleRock] Initialized rock obstacle.");

        if (firstBreakPart == null)
            Debug.LogWarning("[DestructibleRock] First break part NOT assigned!");

        if (remainingParts == null || remainingParts.Length == 0)
            Debug.LogWarning("[DestructibleRock] Remaining parts NOT assigned!");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[DestructibleRock] Trigger hit by: " + other.name);

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[DestructibleRock] Not master client. Ignoring.");
            return;
        }

        if (other.CompareTag("Earth"))
        {
            Debug.Log("[DestructibleRock] Earth hit detected, applying damage...");
            photonView.RPC("RPC_ApplyHit", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_ApplyHit()
    {
        hitCount++;

        Debug.Log("[DestructibleRock] Hit received. Count = " + hitCount);

        // 🔊 Reproducir SFX RockBreaking
        Debug.Log("[DestructibleRock] Playing RockBreaking SFX...");
        SoundManager.Instance.Play(SfxKey.RockBreaking, transform);

        // 1st hit → remove the first big rock part
        if (hitCount == 1)
        {
            if (firstBreakPart != null)
            {
                firstBreakPart.SetActive(false);
                Debug.Log("[DestructibleRock] First rock part disabled.");
            }
        }

        // 2nd hit → remove all remaining parts
        if (hitCount >= hitsNeeded)
        {
            Debug.Log("[DestructibleRock] Rock fully destroyed!");

            foreach (GameObject part in remainingParts)
            {
                if (part != null)
                    part.SetActive(false);
            }

            if (PhotonNetwork.IsMasterClient)
            {
                Invoke(nameof(RemoveRock), destroyDelay);
            }
        }
    }

    void RemoveRock()
    {
        Debug.Log("[DestructibleRock] Removing rock from scene.");
        PhotonNetwork.Destroy(gameObject);
    }
}
