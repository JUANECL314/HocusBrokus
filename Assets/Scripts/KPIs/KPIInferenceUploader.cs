using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class InferencePlayerDto
{
    public string userId;
    public float pHigh;
    public float pMedium;
    public float pLow;
}

[Serializable]
public class InferenceUploadDto
{
    public string matchId;
    public float teamCoordination;
    public float teamLeadership;
    public float teamRecovery;
    public InferencePlayerDto[] players;

    // opcional: snapshot crudo de los puzzles
    public Puzzle1Snapshot rawPuzzle1;
    public Puzzle2Snapshot rawPuzzle2;
}

[Serializable]
public class Puzzle1Snapshot
{
    public float totalTime;
    public float coordinationPct;
    public int cooperationEvents;
    // etc, si quieres más
}

[Serializable]
public class Puzzle2Snapshot
{
    public float timeFirstHitToDoorOpen;
    public float stableTime;
    public float pausedTime;
    public int randomFalls;
    public int waterCooldowns;
    public int earthResets;
    public int overheatResets;
    public int doorPauseEvents;
}
