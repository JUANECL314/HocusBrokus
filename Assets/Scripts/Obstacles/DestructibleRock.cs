using UnityEngine;
using Photon.Pun;

public class DestructibleRock : MonoBehaviourPun
{
    [Header("Rock Parts")]
    public GameObject firstBreakPart;
    public GameObject[] remainingParts;

    [Header("Settings")]
    public int hitsNeeded = 2;
    public float destroyDelay = 0.5f;

    private int hitCount = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Earth"))
        {
            // 🌎 Any player can damage it
            photonView.RPC("RPC_ApplyHit", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void RPC_ApplyHit()
    {
        hitCount++;

        // 🔊 SFX
        SoundManager.Instance.Play(SfxKey.RockBreaking, transform);

        // 🔨 First big hit
        if (hitCount == 1 && firstBreakPart != null)
            firstBreakPart.SetActive(false);

        // 🪨 Final destruction
        if (hitCount >= hitsNeeded)
        {
            foreach (GameObject part in remainingParts)
                if (part != null) part.SetActive(false);

            // 🧼 Small delay for VFX → then disappear for everyone
            Invoke(nameof(DeactivateRock), destroyDelay);
        }
    }

    void DeactivateRock()
    {
        gameObject.SetActive(false);
    }
}
