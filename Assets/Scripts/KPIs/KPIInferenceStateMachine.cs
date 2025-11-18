using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Máquina de estados que recibe los KPIs de Puzzle 1 (espejos) y Puzzle 2 (engranes),
/// calcula una inferencia probabilística sencilla y expone un resultado listo
/// para la pantalla de endorsements.
/// </summary>
public class KPIInferenceStateMachine : MonoBehaviour
{
    public static KPIInferenceStateMachine Instance { get; private set; }

    public enum State
    {
        Idle,               // Aún no hay datos
        WaitingPuzzle1,     // Ya llegó P2, falta P1
        WaitingPuzzle2,     // Ya llegó P1, falta P2
        Ready,              // Resultado calculado, listo para usar
        Sent                // Ya fue consumido por la pantalla de endorsements
    }

    [Header("Estado actual (debug)")]
    public State CurrentState = State.Idle;

    [Header("Resultados de puzzles (debug)")]
    public Puzzle1KPIResult lastPuzzle1;
    public Puzzle2KPIResult lastPuzzle2;

    [Header("Resultado final de inferencia")]
    public InferenceResult CurrentResult;

    bool hasPuzzle1 = false;
    bool hasPuzzle2 = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);      // para que sobreviva entre escenas
    }

    // --------------------------------------------------------------------
    // Hooks que llaman los trackers
    // --------------------------------------------------------------------

    public void OnPuzzle1Completed(Puzzle1KPIResult result)
    {
        lastPuzzle1 = result;
        hasPuzzle1 = true;

        if (!hasPuzzle2)
            CurrentState = State.WaitingPuzzle2;

        TryCompute();
    }

    public void OnPuzzle2Completed(Puzzle2KPIResult result)
    {
        lastPuzzle2 = result;
        hasPuzzle2 = true;

        if (!hasPuzzle1)
            CurrentState = State.WaitingPuzzle1;

        TryCompute();
    }

    void TryCompute()
    {
        if (!hasPuzzle1 || !hasPuzzle2) return;
        if (CurrentState == State.Ready || CurrentState == State.Sent) return;

        ComputeInference();
    }

    // --------------------------------------------------------------------
    // Inferencia "probabilística" (scores 0..1) a partir de ambos puzzles
    // --------------------------------------------------------------------
    void ComputeInference()
    {
        if (lastPuzzle1 == null || lastPuzzle2 == null)
        {
            Debug.LogWarning("[Inference] Falta algún KPIResult, no se puede calcular.");
            return;
        }

        // --- Puzzle 1 (espejos) ---
        float timeScore = Mathf.Clamp01(60f / (lastPuzzle1.totalTime + 1f)); // <=60s ~ bueno
        float coordScore = Mathf.Clamp01(lastPuzzle1.coordinationPct / 100f); // 0..1
        float coopScore = Mathf.Clamp01(lastPuzzle1.cooperationEvents / 10f); // 10+ eventos = 1
        float balanceScore = 1f - Mathf.Clamp01(lastPuzzle1.stdDevParticipation / 10f); // menor STD = mejor

        // --- Puzzle 2 (engranes) ---
        float totalGearTime = lastPuzzle2.stableTime + lastPuzzle2.pausedTime;
        float stabilityScore = (totalGearTime > 0f)
            ? Mathf.Clamp01(lastPuzzle2.stableTime / totalGearTime)
            : 0f;

        float chaosEvents = lastPuzzle2.randomFalls + lastPuzzle2.overheatResets;
        float chaosPenalty = Mathf.Clamp01(chaosEvents / 10f);

        float recoveryScore = Mathf.Clamp01(
            (lastPuzzle2.waterCooldowns + lastPuzzle2.earthResets) / (chaosEvents + 1f)
        );

        // --- Scores de equipo ---
        float teamCoordination = Mathf.Clamp01(
            0.4f * coordScore +
            0.3f * stabilityScore +
            0.3f * balanceScore
        );

        float teamLeadership = Mathf.Clamp01(
            0.5f * coopScore +
            0.3f * recoveryScore +
            0.2f * timeScore
        );

        float teamRecovery = Mathf.Clamp01(
            recoveryScore * (1f - chaosPenalty)
        );

        // --- Per player leadership ---
        var playerScores = new List<PlayerLeadershipScore>();
        int maxActions = 0;
        foreach (var kv in lastPuzzle1.participationByActor)
            if (kv.Value > maxActions) maxActions = kv.Value;
        if (maxActions <= 0) maxActions = 1;

        foreach (var kv in lastPuzzle1.participationByActor)
        {
            string id = kv.Key;
            float actionsNorm = Mathf.Clamp01((float)kv.Value / maxActions);

            // liderazgo "bruto": más acciones + mejor coordinación global
            float raw = Mathf.Clamp01(0.7f * actionsNorm + 0.3f * teamLeadership);

            // convertir a pseudo-probabilidades (alto / medio / bajo)
            float pHigh = Mathf.Clamp01(raw);
            float pLow = Mathf.Clamp01(1f - raw);
            float pMid = 1f - Mathf.Abs(raw - 0.5f) * 2f;   // pico en 0.5

            // normalizar
            float sum = pHigh + pMid + pLow;
            if (sum <= 0f) sum = 1f;
            pHigh /= sum;
            pMid /= sum;
            pLow /= sum;

            playerScores.Add(new PlayerLeadershipScore
            {
                actorId = id,
                pHigh = pHigh,
                pMedium = pMid,
                pLow = pLow
            });
        }

        CurrentResult = new InferenceResult
        {
            teamCoordination = teamCoordination,
            teamLeadership = teamLeadership,
            teamRecovery = teamRecovery,
            players = playerScores
        };

        CurrentState = State.Ready;

        Debug.Log($"[Inference] Calculado. Coord={teamCoordination:F2}, Leader={teamLeadership:F2}, Recovery={teamRecovery:F2}");
    }

    // --------------------------------------------------------------------
    // API para la pantalla de endorsements
    // --------------------------------------------------------------------

    /// <summary>
    /// Indica si ya hay un resultado listo para usarse.
    /// </summary>
    public bool HasResultReady => CurrentState == State.Ready && CurrentResult != null;

    /// <summary>
    /// Marca que el resultado ya fue consumido (por ejemplo por la pantalla de endorsements).
    /// No es obligatorio, pero ayuda a depurar.
    /// </summary>
    public void MarkResultAsSent()
    {
        if (HasResultReady)
            CurrentState = State.Sent;
    }

    /// <summary>
    /// Devuelve el ID del jugador con mayor probabilidad de liderazgo alto.
    /// </summary>
    public string GetBestLeaderId()
    {
        if (CurrentResult == null || CurrentResult.players == null || CurrentResult.players.Count == 0)
            return null;

        PlayerLeadershipScore best = CurrentResult.players[0];
        foreach (var p in CurrentResult.players)
        {
            if (p.pHigh > best.pHigh)
                best = p;
        }
        return best.actorId;
    }

    // --------------------------------------------------------------------
    // Tipos de datos
    // --------------------------------------------------------------------

    [Serializable]
    public class InferenceResult
    {
        [Range(0f, 1f)] public float teamCoordination;
        [Range(0f, 1f)] public float teamLeadership;
        [Range(0f, 1f)] public float teamRecovery;
        public List<PlayerLeadershipScore> players;
    }

    [Serializable]
    public class PlayerLeadershipScore
    {
        public string actorId;
        [Range(0f, 1f)] public float pHigh;
        [Range(0f, 1f)] public float pMedium;
        [Range(0f, 1f)] public float pLow;
    }
}

/// <summary>
/// Resultado empaquetado del KPITracker (Puzzle espejos / Puzzle 1).
/// </summary>
[Serializable]
public class Puzzle1KPIResult
{
    public float totalTime;
    public float coordinationPct;
    public int cooperationEvents;
    public Dictionary<string, int> participationByActor;
    public float stdDevParticipation;
}

/// <summary>
/// Resultado empaquetado del KPIPuzzle2Tracker (Puzzle engranes / Puzzle 2).
/// </summary>
[Serializable]
public class Puzzle2KPIResult
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
