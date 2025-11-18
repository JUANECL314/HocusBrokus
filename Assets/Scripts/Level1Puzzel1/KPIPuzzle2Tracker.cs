using UnityEngine;
using Photon.Pun;

/// <summary>
/// Tracker de KPIs para el puzzle de engranes (Fuego + Viento + Agua + Tierra).
/// Solo acumula datos y los imprime por Debug.Log al abrir la puerta.
/// Más adelante puedes mandar estos datos a un backend o agregarlos a un modelo bayesiano.
/// </summary>
public class KPIPuzzle2Tracker : MonoBehaviourPun
{
    public static KPIPuzzle2Tracker I { get; private set; }

    [Header("Autoridad / Duplicados")]
    [Tooltip("Si está en true, solo el MasterClient acumula KPIs cuando hay Photon.")]
    public bool onlyMaster = true;

    [Header("Tiempo")]
    [Tooltip("Tiempo desde el primer impacto de elemento hasta la combinación correcta Fire+Wind.")]
    public float timeFirstHitToActivation = -1f;

    [Tooltip("Tiempo total desde el primer impacto hasta abrir la puerta.")]
    public float timeFirstHitToDoorOpen = -1f;

    [Tooltip("Tiempo que el puzzle estuvo en estado estable (engranes girando, sin pausa).")]
    public float stableTime = 0f;

    [Tooltip("Tiempo total que el puzzle estuvo pausado (engranes caídos / caos).")]
    public float pausedTime = 0f;

    // Internos
    float _firstElementHitTime = -1f;
    float _activationTime = -1f;
    bool _doorOpened = false;

    [Header("Contadores de eventos")]
    [Tooltip("Cuántas veces Fire golpeó el activador.")]
    public int fireHits = 0;

    [Tooltip("Cuántas veces Wind golpeó el activador.")]
    public int windHits = 0;

    [Tooltip("Veces que se consiguió la combinación Fire+Wind para activar el puzzle.")]
    public int activationAttempts = 0;

    [Tooltip("Número de caídas aleatorias de engranes.")]
    public int randomFalls = 0;

    [Tooltip("Cuántas veces Agua enfrió engranes.")]
    public int waterCooldowns = 0;

    [Tooltip("Veces que Tierra regresó engranes a su posición inicial.")]
    public int earthResets = 0;

    [Tooltip("Veces que se disparó un sobrecalentamiento global (ResetFromOverheat...).")]
    public int overheatResets = 0;

    [Tooltip("Cuántas veces la puerta se puso en pausa (DoorPause(true)).")]
    public int doorPauseEvents = 0;


    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        // Si quieres que viva solo en este nivel, NO uses DontDestroyOnLoad.
        // DontDestroyOnLoad(gameObject);
    }

    bool HasAuthority
    {
        get
        {
            if (!PhotonNetwork.IsConnected) return true;
            if (!onlyMaster) return true;
            return PhotonNetwork.IsMasterClient;
        }
    }

    // --------------------------------------------------------------------
    // Llamadas desde otros scripts
    // --------------------------------------------------------------------

    /// <summary>Llamar cuando Fire o Wind golpean el activador.</summary>
    public void RegisterElementHit(string element)
    {
        if (!HasAuthority) return;

        if (_firstElementHitTime < 0f)
            _firstElementHitTime = Time.time;

        if (element == "Fire") fireHits++;
        if (element == "Wind") windHits++;
    }

    /// <summary>Llamar cuando Fire+Wind activan el puzzle por primera vez.</summary>
    public void RegisterPuzzleActivated()
    {
        if (!HasAuthority) return;

        activationAttempts++;

        if (_activationTime < 0f)
        {
            _activationTime = Time.time;
            if (_firstElementHitTime >= 0f)
                timeFirstHitToActivation = _activationTime - _firstElementHitTime;
        }
    }

    /// <summary>Llamar cada frame mientras el puzzle está activo, NO pausado y estable.</summary>
    public void AccumulateStableTime(float deltaTime)
    {
        if (!HasAuthority) return;
        stableTime += deltaTime;
    }

    /// <summary>Llamar cada frame mientras el puzzle está pausado.</summary>
    public void AccumulatePausedTime(float deltaTime)
    {
        if (!HasAuthority) return;
        pausedTime += deltaTime;
    }

    /// <summary>Llamar cuando se dispare una caída aleatoria (RandomFallTick → MakeFall).</summary>
    public void RegisterRandomFall()
    {
        if (!HasAuthority) return;
        randomFalls++;
    }

    /// <summary>Llamar cuando Agua enfría engranes (CoolDown).</summary>
    public void RegisterWaterCooldown()
    {
        if (!HasAuthority) return;
        waterCooldowns++;
    }

    /// <summary>Llamar cuando Tierra devuelve un engrane (ReturnToInitialPosition).</summary>
    public void RegisterEarthReset()
    {
        if (!HasAuthority) return;
        earthResets++;
    }

    /// <summary>Llamar cuando se use ResetFromOverheatAndReturnAll.</summary>
    public void RegisterOverheatReset()
    {
        if (!HasAuthority) return;
        overheatResets++;
    }

    /// <summary>Llamar cuando la puerta del puzzle termine de abrirse.</summary>
    public void RegisterDoorOpened()
    {
        if (!HasAuthority) return;
        if (_doorOpened) return;

        _doorOpened = true;
        if (_firstElementHitTime >= 0f)
            timeFirstHitToDoorOpen = Time.time - _firstElementHitTime;

        DumpToConsole();

        // ---- Empaquetar resultado y enviarlo a la máquina de inferencia ----
        var result = new Puzzle2KPIResult
        {
            timeFirstHitToDoorOpen = timeFirstHitToDoorOpen,
            stableTime = stableTime,
            pausedTime = pausedTime,
            randomFalls = randomFalls,
            waterCooldowns = waterCooldowns,
            earthResets = earthResets,
            overheatResets = overheatResets,
            doorPauseEvents = doorPauseEvents
        };

        KPIInferenceStateMachine.Instance?.OnPuzzle2Completed(result);
    }

    /// <summary>Llamar cuando se llame DoorPause(true).</summary>
    public void RegisterDoorPause()
    {
        if (!HasAuthority) return;
        doorPauseEvents++;
    }

    // --------------------------------------------------------------------
    // Salida
    // --------------------------------------------------------------------

    void DumpToConsole()
    {
        Debug.Log("========== [KPI] Puzzle 2 – Engranes ==========");
        Debug.Log($"Fire hits: {fireHits}, Wind hits: {windHits}, Activations: {activationAttempts}");
        Debug.Log($"Random falls: {randomFalls}, Water cooldowns: {waterCooldowns}, Earth resets: {earthResets}");
        Debug.Log($"Overheat global resets: {overheatResets}, Door pauses: {doorPauseEvents}");
        Debug.Log($"Time first-hit → activation: {timeFirstHitToActivation:F2} s");
        Debug.Log($"Time first-hit → door open: {timeFirstHitToDoorOpen:F2} s");
        Debug.Log($"Stable time (engranes OK): {stableTime:F2} s, Paused time (caos): {pausedTime:F2} s");
        Debug.Log("===============================================");
    }
}
