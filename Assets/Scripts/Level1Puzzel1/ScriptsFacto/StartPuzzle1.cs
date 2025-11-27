using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class StartPuzzle1 : MonoBehaviourPun
{
    //  Activador elemental
    private bool fireHit = false;
    private bool windHit = false;
    private bool puzzleActivated = false;
    private bool overheated = false;
    private string FireLoopId => $"activator_fire_{GetInstanceID()}";

    public float Progress01 => Mathf.Clamp01(doorProgress / Mathf.Max(0.0001f, doorOpenTime));
    public bool IsActivated => puzzleActivated;
    public bool IsPaused => doorPaused;

    //  Activación segura
    private bool _isActivating = false;
    private bool _activateScheduled = false;

    //  Progreso de puerta
    [Header("Door / Progress")]
    public float doorOpenTime = 8f;
    private float doorProgress = 0f;
    private bool doorPaused = false;

    //  Caídas aleatorias
    [Header("Random Falls")]
    public float randomFallCooldown = 5f;
    public float randomFallGrace = 5f;
    private bool canTriggerRandomFall = true;
    private float nextRandomFallAllowedTime = 0f;

    //  Engranajes administrados
    [Header("Gears")]
    public GameObject[] gears;

    PuzzleState currentState = PuzzleState.Init;
    enum PuzzleState
    {
        Init,
        Play,
        Failed
    }

    // ----------------------------------------------------
    void Start()
    {
        Debug.Log("Puzzle inicializado correctamente.");
        StateMachineStatus(PuzzleState.Init);
    }

    // ----------------------------------------------------
    void Update()
    {
        if (puzzleActivated)
        {
            bool stable = AllGearsStable();

            if (stable && doorPaused)
                photonView.RPC("DoorPause", RpcTarget.All, false);

            else if (!stable && !doorPaused)
                photonView.RPC("DoorPause", RpcTarget.All, true);
        }

        if (puzzleActivated && !doorPaused && AllGearsStable())
        {
            float dt = Time.deltaTime;
            doorProgress += dt;

            KPIPuzzle2Tracker.I?.AccumulateStableTime(dt);

            if (doorProgress >= doorOpenTime)
            {
                photonView.RPC("OpenDoor", RpcTarget.All);
                puzzleActivated = false;
            }
        }
        else if (puzzleActivated && doorPaused)
        {
            KPIPuzzle2Tracker.I?.AccumulatePausedTime(Time.deltaTime);
        }

        if (puzzleActivated && canTriggerRandomFall && Time.time >= nextRandomFallAllowedTime)
            StartCoroutine(RandomFallTick());
    }

    // ----------------------------------------------------
    void StateMachineStatus(PuzzleState next)
    {
        currentState = next;

        switch (currentState)
        {
            case PuzzleState.Init:
                break;

            case PuzzleState.Play:
                photonView.RPC("RPC_StartGearPuzzle", RpcTarget.All);
                break;

            case PuzzleState.Failed:
                break;
        }
    }

    // ----------------------------------------------------
    void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine)
        {
            if (other.CompareTag("Fire"))
                photonView.RPC("TriggerElement", RpcTarget.All, "Fire");
            if (other.CompareTag("Wind"))
                photonView.RPC("TriggerElement", RpcTarget.All, "Wind");
            return;
        }

        if (other.CompareTag("Fire"))
            TriggerElement("Fire");
        if (other.CompareTag("Wind"))
            TriggerElement("Wind");
    }

    // ----------------------------------------------------
    [PunRPC]
    void TriggerElement(string element)
    {
        if (element == "Fire" && !fireHit)
        {
            fireHit = true;
            SoundManager.Instance?.StartLoop(FireLoopId, SfxKey.FireIgniteLoop, transform);

            KPIPuzzle2Tracker.I?.RegisterElementHit("Fire");
        }

        if (element == "Wind" && !windHit)
        {
            windHit = true;
            KPIPuzzle2Tracker.I?.RegisterElementHit("Wind");
        }

        if (fireHit && windHit && !puzzleActivated)
            StateMachineStatus(PuzzleState.Play);
    }

    // ----------------------------------------------------
    [PunRPC]
    void RPC_StartGearPuzzle()
    {
        overheated = false;
        puzzleActivated = true;
        doorProgress = 0f;
        doorPaused = false;

        nextRandomFallAllowedTime = Time.time + randomFallGrace;
        canTriggerRandomFall = true;

        SoundManager.Instance?.Play(SfxKey.FireWindBoost, transform);
        SoundManager.Instance?.StopLoop(FireLoopId);

        foreach (GameObject g in gears)
        {
            GearStateMachine sm = g.GetComponent<GearStateMachine>();
            if (sm != null)
                sm.StateMachineStatus(GearStateMachine.GearEnumState.Movement);
        }

        KPIPuzzle2Tracker.I?.RegisterPuzzleActivated();
    }

    // ----------------------------------------------------
    [PunRPC]
    void DoorPause(bool pause)
    {
        doorPaused = pause;

        if (pause)
            KPIPuzzle2Tracker.I?.RegisterDoorPause();
    }

    // ----------------------------------------------------
    bool AllGearsStable()
    {
        if (gears.Length == 0)
            return false;

        foreach (var g in gears)
        {
            var sm = g.GetComponent<GearStateMachine>();
            if (sm == null)
                return false;

            if (!sm.IsRotating || sm.isFalling)
                return false;
        }
        return true;
    }

    // ----------------------------------------------------
    [PunRPC]
    void OpenDoor()
    {
        KPIPuzzle2Tracker.I?.RegisterDoorOpened();
        puzzleActivated = false;
        doorProgress = 0f;
        DoorPause(false);

        StartCoroutine(RotateDoors());
        canTriggerRandomFall = false;
    }

    IEnumerator RotateDoors()
    {
        GameObject[] doors = GameObject.FindGameObjectsWithTag("Puerta");
        float duration = 2f;
        float elapsed = 0f;

        Quaternion[] start = new Quaternion[doors.Length];
        Quaternion[] end = new Quaternion[doors.Length];

        for (int i = 0; i < doors.Length; i++)
        {
            start[i] = doors[i].transform.rotation;
            end[i] = Quaternion.Euler(doors[i].transform.eulerAngles + new Vector3(0f, 90f, 0f));
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null)
                    doors[i].transform.rotation = Quaternion.Slerp(start[i], end[i], t);
            }
            yield return null;
        }
    }

    // ----------------------------------------------------
    IEnumerator RandomFallTick()
    {
        canTriggerRandomFall = false;

        List<GearStateMachine> candidates = new List<GearStateMachine>();
        foreach (GameObject g in gears)
        {
            var sm = g.GetComponent<GearStateMachine>();
            if (sm != null && !sm.isFalling && sm.IsRotating)
                candidates.Add(sm);
        }

        if (candidates.Count > 0)
        {
            GearStateMachine pick = candidates[Random.Range(0, candidates.Count)];
            KPIPuzzle2Tracker.I?.RegisterRandomFall();
            pick.StateMachineStatus(GearStateMachine.GearEnumState.Fall_Shake);
        }

        yield return new WaitForSeconds(randomFallCooldown);
        canTriggerRandomFall = true;
    }
}
