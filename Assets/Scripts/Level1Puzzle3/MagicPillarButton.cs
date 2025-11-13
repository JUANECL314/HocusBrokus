using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Collider))]
public class MagicPillarButton : MonoBehaviourPun
{
    [Header("Element Type")]
    public string elementTag = "Fire"; // Fire, Water, Wind, Earth
    public MagicPillarPuzzleManager puzzleManager;
    public bool useTrigger = true;

    private Renderer rend;
    private Color defaultColor;

    void Start()
    {
        rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        if (rend != null)
            defaultColor = rend.material.color;

        // Try to auto-assign manager
        if (puzzleManager == null)
            puzzleManager = FindObjectOfType<MagicPillarPuzzleManager>();
    }

    void OnMouseEnter()
    {
        if (rend != null)
            rend.material.color = Color.yellow;
    }

    void OnMouseExit()
    {
        if (rend != null)
            rend.material.color = defaultColor;
    }

   void OnTriggerEnter(Collider other)
{
    if (!useTrigger) return;

    if (!IsMagicActivator(other.gameObject))
        return;

    // Try to assign puzzleManager dynamically if null
    if (puzzleManager == null)
        puzzleManager = FindObjectOfType<MagicPillarPuzzleManager>();

    if (puzzleManager == null)
    {
        Debug.LogWarning($"{name}: PuzzleManager not found yet! Ignoring input.");
        return; // exit safely, do not crash
    }

    // Only MasterClient sends the main input
    if (PhotonNetwork.IsMasterClient)
    {
        puzzleManager.RegisterInput(elementTag);

        // RPC for other clients
        if (photonView != null && photonView.IsMine)
            photonView.RPC(nameof(RPC_RegisterInput), RpcTarget.OthersBuffered, elementTag);
    }
}

    void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;

        if (IsMagicActivator(collision.collider.gameObject))
        {
            if (puzzleManager == null)
                puzzleManager = FindObjectOfType<MagicPillarPuzzleManager>();

            if (puzzleManager == null)
            {
                Debug.LogWarning($"{name}: PuzzleManager still null!");
                return;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                puzzleManager.RegisterInput(elementTag);
                photonView.RPC(nameof(RPC_RegisterInput), RpcTarget.OthersBuffered, elementTag);
            }
        }
    }

    bool IsMagicActivator(GameObject go)
    {
        string tag = go.tag;
        return tag == "Player" || tag == "Fire" || tag == "Water" || tag == "Earth" || tag == "Wind";
    }

    [PunRPC]
    void RPC_RegisterInput(string element)
    {
        if (puzzleManager != null)
            puzzleManager.RegisterInput(element);
    }
}
