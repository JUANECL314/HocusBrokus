using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class MagicPillarPuzzleManager : MonoBehaviourPun
{
    [Header("Target Code Sequence")]
    public List<string> correctSequence = new List<string>
        { "Fire", "Fire", "Water", "Wind", "Earth", "Fire", "Wind", "Water" };

    [Header("Mirror Alignment Targets")]
    public Transform[] mirrorTargets; // assign scene targets in inspector
    public List<MirrorController> mirrorsToAlign = new List<MirrorController>();

    private List<string> currentInput = new List<string>();
    public float resetDelay = 2f;

    // Called by MirrorController when spawned
    public void RegisterMirror(MirrorController mirror)
    {
        mirrorsToAlign.Add(mirror);

        int index = mirrorsToAlign.Count - 1;
        if (mirrorTargets != null && index < mirrorTargets.Length)
        {
            mirror.alignTarget = mirrorTargets[index];
            Debug.Log($"Mirror {mirror.name} assigned target {mirrorTargets[index].name}");
        }
    }

    public void RegisterInput(string element)
    {
        Debug.Log($"RegisterInput called on MagicPillarPuzzleManager, element: {element}");
        currentInput.Add(element);

        if (!IsPrefixMatch())
        {
            Debug.Log("❌ Incorrect sequence, resetting...");
            ResetSequence();
            return;
        }

        if (currentInput.Count == correctSequence.Count)
        {
            Debug.Log("✅ Correct sequence entered!");
            photonView.RPC(nameof(RPC_OnPuzzleSolved), RpcTarget.AllBuffered);
        }
    }

    bool IsPrefixMatch()
    {
        for (int i = 0; i < currentInput.Count; i++)
        {
            if (i >= correctSequence.Count || currentInput[i] != correctSequence[i])
                return false;
        }
        return true;
    }

    void ResetSequence()
    {
        StopAllCoroutines();
        StartCoroutine(ResetAfterDelay());
    }

    private System.Collections.IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);
        currentInput.Clear();
        Debug.Log("🔄 Sequence reset.");
    }

    [PunRPC]
    void RPC_OnPuzzleSolved()
    {
        Debug.Log("✨ Puzzle solved! Aligning mirrors...");
        foreach (var mirror in mirrorsToAlign)
        {
            if (mirror != null)
                mirror.AlignToTarget();
        }
    }
}
