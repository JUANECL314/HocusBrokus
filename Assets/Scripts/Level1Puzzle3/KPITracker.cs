using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// KPI minimal para 4 métricas:
// 1) Tiempo total del puzzle
// 2) Coordinación: % del tiempo con ≥2 espejos encendidos
// 3) Eventos de cooperación: acciones cercanas entre actores distintos
// 4) Participación por actor + STD (más bajo = más balanceado)
public class KPITracker : MonoBehaviour
{
    public static KPITracker Instance { get; private set; }

    [Header("Ajustes")]
    [Tooltip("Cuánto tiempo (s) se considera 'encendido' un espejo después de recibir láser.")]
    public float litMemoryWindow = 0.15f;

    [Tooltip("Ventana (s) para considerar que dos acciones de actores distintos son cooperación.")]
    public float assistWindow = 2f;

    // Tiempo
    private bool timerStarted = false;
    private float startTime = 0f;
    private float stopTime = 0f;
    private bool puzzleCompleted = false;

    // Espejos encendidos recientemente: mirror -> venceA
    private readonly Dictionary<Transform, float> litUntilByMirror = new Dictionary<Transform, float>();

    // Participación y cooperación
    private readonly Dictionary<string, int> actionsByActor = new Dictionary<string, int>();
    private string lastActorId = null;
    private float lastActorTime = -999f;
    private int cooperationEvents = 0;

    // Coordinación
    private float lastUpdateTime = 0f;
    private float activeTime = 0f;
    private float multiLitTime = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // opcional si quieres persistir entre escenas
    }

    void Update()
    {
        float now = Time.time;

        // Integración de tiempos para coordinación
        if (timerStarted && !puzzleCompleted)
        {
            float dt = (lastUpdateTime > 0f) ? (now - lastUpdateTime) : 0f;
            if (dt > 0f)
            {
                activeTime += dt;
                if (CurrentLitCount() >= 2) multiLitTime += dt;
            }
            lastUpdateTime = now;
        }

        // Limpieza de espejos encendidos expirados
        if (litUntilByMirror.Count > 0)
        {
            _tmpKeys ??= new List<Transform>(8);
            _tmpKeys.Clear();
            foreach (var k in litUntilByMirror.Keys) _tmpKeys.Add(k);
            for (int i = 0; i < _tmpKeys.Count; i++)
            {
                if (litUntilByMirror[_tmpKeys[i]] < now) litUntilByMirror.Remove(_tmpKeys[i]);
            }
        }
    }
    private List<Transform> _tmpKeys;

    private int CurrentLitCount()
    {
        int n = 0; float t = Time.time;
        foreach (var kv in litUntilByMirror) if (kv.Value >= t) n++;
        return n;
    }

    private void EnsureTimerStarted(string reason)
    {
        if (!timerStarted && !puzzleCompleted)
        {
            timerStarted = true;
            startTime = Time.time;
            lastUpdateTime = startTime;
            Debug.Log($"[KPI] Timer iniciado ({reason}).");
        }
    }

    // ---------- Identidad de actor ----------
    private string GetActorId(GameObject actor)
    {
        if (actor == null) return "Unknown";
        var pv = actor.GetComponentInParent<PhotonView>();
        if (pv != null && pv.Owner != null) return $"P{pv.OwnerActorNr}";
        if (!string.IsNullOrEmpty(actor.tag) && actor.tag != "Untagged") return $"Tag:{actor.tag}";
        return actor.name;
    }

    private void CountActorAction(GameObject actor)
    {
        string id = GetActorId(actor);
        if (!actionsByActor.ContainsKey(id)) actionsByActor[id] = 0;
        actionsByActor[id]++;

        float now = Time.time;
        if (!string.IsNullOrEmpty(lastActorId) && lastActorId != id && (now - lastActorTime) <= assistWindow)
            cooperationEvents++;

        lastActorId = id;
        lastActorTime = now;
    }

    // ========== HOOKS PÚBLICOS ==========

    /// Llamar cuando el rayo pega en un espejo (ya lo hace tu LaserBeam).
    public void MarkMirrorLit(Transform mirror)
    {
        EnsureTimerStarted("primer impacto de láser");
        litUntilByMirror[mirror] = Time.time + litMemoryWindow;
    }

    /// Llamar cuando alguien presiona el botón (desde MirrorButton, pasando el actor).
    public void RegisterButtonPress(GameObject buttonOrMirror, GameObject actor = null)
    {
        EnsureTimerStarted("primera acción (button)");
        if (actor != null) CountActorAction(actor);
    }

    /// Si quieres, puedes llamarlo también tras una rotación o move up con actor (opcional).
    public void RegisterRotationOrMoveUp(GameObject actor = null)
    {
        EnsureTimerStarted("acción (rotación/move up)");
        if (actor != null) CountActorAction(actor);
    }

    /// Llamar cuando el puzzle se complete (al cumplir condiciones en LaserTarget).
    public void OnPuzzleCompleted()
    {
        if (puzzleCompleted) return;
        puzzleCompleted = true;
        stopTime = Time.time;

        float elapsed = timerStarted ? (stopTime - startTime) : 0f;
        float coordPct = (activeTime > 0f) ? (multiLitTime / activeTime) * 100f : 0f;

        // Participación + STD
        string participation = "";
        double sum = 0, sumSq = 0; int n = 0;
        foreach (var kv in actionsByActor)
        {
            participation += $"  - {kv.Key}: {kv.Value}\n";
            sum += kv.Value; sumSq += kv.Value * kv.Value; n++;
        }
        double mean = (n > 0) ? sum / n : 0;
        double variance = (n > 0) ? (sumSq / n - mean * mean) : 0;
        double stddev = System.Math.Sqrt(System.Math.Max(0, variance));

        Debug.Log(
            "[KPI] Puzzle COMPLETADO" +
            $"\n  Tiempo total: {elapsed:F2}s" +
            $"\n  Coordinación (≥2 espejos encendidos): {coordPct:F1}% del tiempo activo" +
            $"\n  Eventos de cooperación (acciones cercanas entre actores distintos): {cooperationEvents}" +
            $"\n  Participación por actor (STD más baja = más balanceado):\n{participation}" +
            $"\n  STD participación: {stddev:F2}"
        );
    }
}
