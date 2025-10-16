using System.Collections.Generic;
using UnityEngine;

public class KPITracker : MonoBehaviour
{
    public static KPITracker Instance { get; private set; }

    [Header("Config")]
    [Tooltip("Ventana de tiempo para considerar que un espejo 'está iluminado' (segundos)")]
    public float litMemoryWindow = 0.15f;

    private bool timerStarted = false;
    private float startTime = 0f;
    private float stopTime = 0f;
    private bool puzzleCompleted = false;

    // Guardamos mirrors iluminados recientemente: mirror -> timeUntil
    private readonly Dictionary<Transform, float> litUntilByMirror = new Dictionary<Transform, float>();

    // Contadores
    public int attemptsLitRotations { get; private set; } = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Opcional: si quieres que persista entre escenas
        // DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Purga suave de entradas expiradas
        if (litUntilByMirror.Count == 0) return;

        var keys = new List<Transform>(litUntilByMirror.Keys);
        float t = Time.time;
        foreach (var k in keys)
        {
            if (litUntilByMirror[k] < t) litUntilByMirror.Remove(k);
        }
    }

    /// Llamado por LaserBeam cuando el rayo golpea un espejo.
    public void MarkMirrorLit(Transform mirror)
    {
        float until = Time.time + litMemoryWindow;
        litUntilByMirror[mirror] = until;
        // Inicia timer en el primer evento real del puzzle
        if (!timerStarted && !puzzleCompleted)
        {
            timerStarted = true;
            startTime = Time.time;
            Debug.Log("[KPI] Timer iniciado (primer impacto de láser en espejo).");
        }
    }

    /// Llamado por MirrorController cuando se rota un espejo.
    public void RegisterRotation(Transform mirror)
    {
        if (!timerStarted && !puzzleCompleted)
        {
            timerStarted = true;
            startTime = Time.time;
            Debug.Log("[KPI] Timer iniciado (primera rotación).");
        }

        // Suma intento sólo si ese espejo está (o estuvo hace ms) iluminado
        if (IsMirrorCurrentlyLit(mirror))
        {
            attemptsLitRotations++;
            Debug.Log($"[KPI] Intento contado (rotación con espejo iluminado). Total: {attemptsLitRotations}");
        }
        else
        {
            // Si quisieras contar también las rotaciones a oscuras, aquí podrías llevar otro contador.
            // Debug.Log("[KPI] Rotación sin luz: no cuenta como intento.");
        }
    }

    private bool IsMirrorCurrentlyLit(Transform mirror)
    {
        return litUntilByMirror.TryGetValue(mirror, out float until) && Time.time <= until;
    }

    /// Llamado por LaserTarget al completar el puzzle.
    public void OnPuzzleCompleted()
    {
        if (puzzleCompleted) return;
        puzzleCompleted = true;
        stopTime = Time.time;

        float elapsed = timerStarted ? (stopTime - startTime) : 0f;
        Debug.Log($"[KPI] Puzzle completado. Tiempo: {elapsed:F2}s | Intentos (rotaciones con luz): {attemptsLitRotations}");

        // Aquí puedes despachar a UI / Analytics / Persistencia:
        // - Enviar a un GameManager
        // - Guardar en PlayerPrefs / archivo / backend
        // - Disparar UnityEvents públicos, etc.
    }
}
