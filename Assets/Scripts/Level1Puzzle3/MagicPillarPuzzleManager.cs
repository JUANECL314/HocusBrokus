using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class MagicPillarPuzzleManager : MonoBehaviourPun
{
    [Header("Target Code Sequence")]
    public List<string> correctSequence = new List<string>
        { "Fire", "Fire", "Water", "Wind", "Earth", "Fire", "Wind", "Water" };

    [Header("Mirror Alignment Targets")]
    public Transform[] mirrorTargets;
    public List<MirrorController> mirrorsToAlign = new List<MirrorController>();

    [Header("Laser Beams")]
    public LaserBeam halfwayLaser;
    public LaserBeam fullLaser;

    private List<string> currentInput = new List<string>();
    public float resetDelay = 2f;

    // -------------------------------------------------------------
    //  Public: Buttons call THIS via RPC (MasterClient only)
    // -------------------------------------------------------------
    [PunRPC]
    void RPC_RegisterInput(string element)
    {
        if (!PhotonNetwork.IsMasterClient)
            return; // Safety: only Master runs puzzle logic

        RegisterInputInternal(element);
    }

    // -------------------------------------------------------------
    //  INTERNAL SEQUENCE PROCESSING (MasterClient only)
    // -------------------------------------------------------------
    private void RegisterInputInternal(string element)
    {
        Debug.Log($"[MASTER] RegisterInput: {element}");

        if (currentInput.Count >= correctSequence.Count)
            return;

        // Wrong input BEFORE adding
        if (element != correctSequence[currentInput.Count])
        {
            Debug.Log("[MASTER] ❌ Wrong element → resetting!");

            // Play SFX globally
            photonView.RPC("RPC_PlayWrong", RpcTarget.All);

            ResetSequenceInternal();
            return;
        }

        // Play correct ding globally
        photonView.RPC("RPC_PlayCorrect", RpcTarget.All);

        currentInput.Add(element);

        // Halfway
        if (currentInput.Count == correctSequence.Count / 2)
        {
            photonView.RPC("RPC_EnableHalfway", RpcTarget.All);
        }

        // Puzzle complete
        if (currentInput.Count == correctSequence.Count)
        {
            Debug.Log("[MASTER] ✅ Correct sequence entered!");
            photonView.RPC(nameof(RPC_OnPuzzleSolved), RpcTarget.AllBuffered);
        }
    }

    // -------------------------------------------------------------
    //  RESET (Master triggers, all clients receive)
    // -------------------------------------------------------------
    void ResetSequenceInternal()
    {
        StopAllCoroutines();
        StartCoroutine(ResetAfterDelay());
    }

    private System.Collections.IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);

        currentInput.Clear();
        photonView.RPC("RPC_ResetVisuals", RpcTarget.All);

        Debug.Log("[MASTER] 🔄 Sequence reset.");
    }

    // -------------------------------------------------------------
    //  RPCs for syncing visuals + sounds across network
    // -------------------------------------------------------------
    [PunRPC]
    void RPC_PlayWrong()
    {
        SoundManager.Instance.Play(SfxKey.WrongAnswerBuzz, transform);
    }

    [PunRPC]
    void RPC_PlayCorrect()
    {
        SoundManager.Instance.Play(SfxKey.CorrectAnswerDing, transform);
    }

    [PunRPC]
    void RPC_EnableHalfway()
    {
        halfwayLaser?.SetLaserActive(true);
        SoundManager.Instance.Play(SfxKey.TargetFastPing, transform);
        Debug.Log("🔹 Halfway laser activated!");
    }

    [PunRPC]
    void RPC_ResetVisuals()
    {
        halfwayLaser?.SetLaserActive(false);
        fullLaser?.SetLaserActive(false);
    }

    // -------------------------------------------------------------
    //  PUZZLE SOLVED
    // -------------------------------------------------------------
    [PunRPC]
    void RPC_OnPuzzleSolved()
    {
        Debug.Log("✨ Puzzle solved! Aligning mirrors...");
        fullLaser?.SetLaserActive(true);

        SoundManager.Instance.Play(SfxKey.TargetIlluminate, transform);

        foreach (var mirror in mirrorsToAlign)
        {
            if (mirror != null)
                mirror.AlignToTarget();
        }
    }

    // -------------------------------------------------------------
    //  Utility
    // -------------------------------------------------------------
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

    public int GetCurrentInputCount() => currentInput.Count;

    public bool WouldInputBeCorrect(string element)
    {
        if (currentInput.Count >= correctSequence.Count)
            return false;

        return element == correctSequence[currentInput.Count];
    }
}
