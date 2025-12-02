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

    private void OnTriggerEnter(Collider other)
    {
        if (!isBurning && other.CompareTag("Fire"))
        {
            // 🔥 Any player can ignite
            photonView.RPC("RPC_StartBurning", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void RPC_StartBurning()
    {
        if (isBurning) return;
        isBurning = true;

        // 🔊 SOUND
        SoundManager.Instance.Play(SfxKey.BurningTree, transform);

        if (fireVFX != null)
            fireVFX.SetActive(true);

        Invoke(nameof(DeactivateTree), burnDuration);
    }

    void DeactivateTree()
    {
        // 🪵 Simply hide the tree everywhere
        gameObject.SetActive(false);
    }
}
