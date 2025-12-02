using System.Collections.Generic;
using UnityEngine;

public class InferenceMockTester : MonoBehaviour
{
    [Header("Opciones de mock")]
    public string mockMatchId = "mock-match-001";
    public bool autoRunOnStart = true;

    void Start()
    {
        if (autoRunOnStart)
        {
            RunMockOnce();
        }
    }

    [ContextMenu("Run Mock Once")]
    public void RunMockOnce()
    {
        // 1) Asegurar que la state machine exista
        var sm = KPIInferenceStateMachine.Instance;
        if (sm == null)
        {
            var go = new GameObject("KPIInferenceStateMachine");
            sm = go.AddComponent<KPIInferenceStateMachine>();
        }

        // 2) Asegurar que el uploader exista
        var uploader = InferenceUploader.Instance;
        if (uploader == null)
        {
            var go = new GameObject("InferenceUploader");
            uploader = go.AddComponent<InferenceUploader>();
        }

        // 3) Construir resultados falsos de Puzzle 1 (espejos)
        var p1 = new Puzzle1KPIResult
        {
            totalTime = 75f,          // 75 segundos
            coordinationPct = 68f,    // 68% del tiempo con ≥2 espejos encendidos
            cooperationEvents = 5,
            participationByActor = new Dictionary<string, int>
            {
                { "P1", 12 },
                { "P2", 9 },
                { "P3", 7 },
                { "P4", 4 }
            },
            stdDevParticipation = 3.1f
        };

        // 4) Construir resultados falsos de Puzzle 2 (engranes)
        var p2 = new Puzzle2KPIResult
        {
            timeFirstHitToDoorOpen = 90f,
            stableTime = 60f,
            pausedTime = 30f,
            randomFalls = 3,
            waterCooldowns = 4,
            earthResets = 2,
            overheatResets = 1,
            doorPauseEvents = 2
        };

        // 5) Enviar a la state machine (esto dispara la inferencia)
        sm.OnPuzzle1Completed(p1);
        sm.OnPuzzle2Completed(p2);

        if (!sm.HasResultReady)
        {
            Debug.LogWarning("[InferenceMockTester] La inferencia no quedó lista. Revisa KPIInferenceStateMachine.");
            return;
        }

        var res = sm.CurrentResult;
        Debug.Log($"[InferenceMockTester] Resultado mock listo. " +
                  $"Coord={res.teamCoordination:F2}, Leader={res.teamLeadership:F2}, Recov={res.teamRecovery:F2}");

        // 6) Mandar al backend
        InferenceUploader.Instance.SendInference(mockMatchId);
    }
}
