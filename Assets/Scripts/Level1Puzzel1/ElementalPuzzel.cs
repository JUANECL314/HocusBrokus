using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class ElementalPuzzle : MonoBehaviourPun
{
    // --- Activador elemental ---
    private bool fireHit = false;
    private bool windHit = false;
    private bool puzzleActivated = false;
    private bool overheated = false;  // Sobrecalentamiento
    private string FireLoopId => $"activator_fire_{GetInstanceID()}";

    // Getters para UI
    public float Progress01 => Mathf.Clamp01(doorProgress / Mathf.Max(0.0001f, doorOpenTime));
    public bool IsActivated => puzzleActivated;
    public bool IsPaused => doorPaused;

    private bool _isActivating = false;
    private bool _activateScheduled = false;

    // --- Puerta / progreso ---
    [Header("Door / Progress")]
    public float doorOpenTime = 8f;      // segundos necesarios en condiciones válidas
    private float doorProgress = 0f;
    private bool doorPaused = false;

    // --- Caidas aleatorias ---
    [Header("Random Falls")]
    public float randomFallCooldown = 5f;    // CD entre caidas
    public float randomFallGrace = 5f;       // tiempo extra al arrancar
    private bool canTriggerRandomFall = true;
    private float nextRandomFallAllowedTime = 0f; // tiempo extra

    void Update()
    {
        // Pausar/Reanudar puzzles
        if (puzzleActivated)
        {
            bool stable = AllGearsStable();

            // Si volvió todo a estable y estaba en pausa → reanudar
            if (stable && doorPaused)
                DoorPause(false);

            // Si dejo de estar estable y no estaba en pausa → pausar
            else if (!stable && !doorPaused)
                DoorPause(true);
        }

        // Avanza progreso solo si está activo, no en pausa y todos los engranajes estables
        if (puzzleActivated && !doorPaused && AllGearsStable())
        {
            float dt = Time.deltaTime;
            doorProgress += dt;

            // KPI: tiempo estable acumulado
            KPIPuzzle2Tracker.I?.AccumulateStableTime(dt);

            if (doorProgress >= doorOpenTime)
            {
                OpenDoor();
                puzzleActivated = false;
            }
        }
        else if (puzzleActivated && doorPaused)
        {
            // KPI: tiempo en pausa (caos)
            KPIPuzzle2Tracker.I?.AccumulatePausedTime(Time.deltaTime);
        }

        // Caidas aleatorias con gracia + cooldown
        if (puzzleActivated && canTriggerRandomFall && Time.time >= nextRandomFallAllowedTime)
            StartCoroutine(RandomFallTick());
    }

    void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine)
        {
            // Si este cliente NO tiene autoridad sobre el puzzle,
            // manda la acción a todos.
            if (other.CompareTag("Fire"))
                photonView.RPC("TriggerElement", RpcTarget.All, "Fire");

            if (other.CompareTag("Wind"))
                photonView.RPC("TriggerElement", RpcTarget.All, "Wind");

            return;
        }

        // Si SÍ tiene autoridad (el host), ejecuta directamente:
        if (other.CompareTag("Fire"))
            TriggerElement("Fire");
        if (other.CompareTag("Wind"))
            TriggerElement("Wind");
    }

    [PunRPC]
    void TriggerElement(string element)
    {
        if (element == "Fire" && !fireHit)
        {
            fireHit = true;
            SoundManager.Instance?.StartLoop(FireLoopId, SfxKey.FireIgniteLoop, transform);

            // KPI: primer impacto Fire
            KPIPuzzle2Tracker.I?.RegisterElementHit("Fire");
        }

        if (element == "Wind" && !windHit)
        {
            windHit = true;

            // KPI: primer impacto Wind
            KPIPuzzle2Tracker.I?.RegisterElementHit("Wind");
        }

        // Si ambos elementos están presentes, activa el puzzle
        if (fireHit && windHit && !puzzleActivated)
        {
            overheated = false;
            puzzleActivated = true;
            doorProgress = 0f;
            doorPaused = false;

            nextRandomFallAllowedTime = Time.time + randomFallGrace;
            canTriggerRandomFall = true;

            SoundManager.Instance?.Play(SfxKey.FireWindBoost, transform);
            SoundManager.Instance?.StopLoop(FireLoopId);

            var gears = GameObject.FindGameObjectsWithTag("Engranaje");
            foreach (var go in gears)
            {
                var gb = go.GetComponent<GearBehavior>();
                if (gb != null) gb.SetAutoReactivateOnLand(true);
            }

            // KPI: combinación Fire+Wind que activa el puzzle
            KPIPuzzle2Tracker.I?.RegisterPuzzleActivated();

            ScheduleActivateGears();
        }
    }

    [PunRPC]
    public void DoorPause(bool pause)
    {
        doorPaused = pause;

        if (pause)
        {
            // KPI: evento de pausa
            KPIPuzzle2Tracker.I?.RegisterDoorPause();
        }
    }

    [PunRPC]
    public void ResetFromOverheatAndReturnAll()
    {
        overheated = true;

        // KPI: overheat global
        KPIPuzzle2Tracker.I?.RegisterOverheatReset();

        // limpiar flags de activacion (para obligar a relanzar Fuego+Viento)
        fireHit = false;
        windHit = false;

        DoorPause(true);
        puzzleActivated = false;
        doorProgress = 0f;

        SoundManager.Instance?.StopLoop(FireLoopId);

        var gears = GameObject.FindGameObjectsWithTag("Engranaje");
        foreach (var go in gears)
        {
            var gb = go.GetComponent<GearBehavior>();
            if (gb == null) continue;

            gb.SetAutoReactivateOnLand(false); // tras overheat NO se reactivan solos al aterrizar
            gb.StopRotation();
            gb.ResetToInitialPosition(true);
        }
    }

    [PunRPC]
    bool AllGearsStable()
    {
        var gears = GameObject.FindGameObjectsWithTag("Engranaje");
        if (gears.Length == 0) return false;

        foreach (var g in gears)
        {
            var gb = g.GetComponent<GearBehavior>();
            if (gb == null) return false;

            if (!gb.IsRotating || gb.isFalling) return false;
        }
        return true;
    }

    [PunRPC]
    void ActivateGears()
    {
        if (_isActivating) return;
        _isActivating = true;
        try
        {
            var gears = GameObject.FindGameObjectsWithTag("Engranaje");
            foreach (var gear in gears)
            {
                var gb = gear.GetComponent<GearBehavior>();
                if (gb != null) gb.StartRotation();
            }
        }
        finally
        {
            _isActivating = false;
        }
    }

    [PunRPC]
    void ScheduleActivateGears()
    {
        if (_activateScheduled) return;
        _activateScheduled = true;
        StartCoroutine(_ActivateNextFrame());
    }

    [PunRPC]
    IEnumerator _ActivateNextFrame()
    {
        yield return null;
        _activateScheduled = false;
        ActivateGears();
    }

    [PunRPC]
    void OpenDoor()
    {
        // KPI: puerta abierta (puzzle resuelto)
        KPIPuzzle2Tracker.I?.RegisterDoorOpened();

        // Inicia la rotación de las puertas en lugar de destruirlas
        StartCoroutine(RotateDoors());

        // Reset de estado del puzzle
        puzzleActivated = false;
        doorProgress = 0f;
        DoorPause(false);

        // (Opcional) parar caídas aleatorias
        canTriggerRandomFall = false;
    }

    [PunRPC]
    IEnumerator RotateDoors()
    {
        GameObject[] doors = GameObject.FindGameObjectsWithTag("Puerta");
        float duration = 2f; // segundos que tarda en abrirse
        float elapsed = 0f;

        Quaternion[] startRot = new Quaternion[doors.Length];
        Quaternion[] endRot = new Quaternion[doors.Length];

        for (int i = 0; i < doors.Length; i++)
        {
            startRot[i] = doors[i].transform.rotation;
            endRot[i] = Quaternion.Euler(doors[i].transform.eulerAngles + new Vector3(0f, 90f, 0f));
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null)
                    doors[i].transform.rotation = Quaternion.Slerp(startRot[i], endRot[i], t);
            }
            yield return null;
        }
    }

    [PunRPC]
    IEnumerator RandomFallTick()
    {
        canTriggerRandomFall = false;

        // elige engranajes elegibles (girando y no cayendo)
        var candidates = new List<GearBehavior>();
        foreach (var go in GameObject.FindGameObjectsWithTag("Engranaje"))
        {
            var gb = go.GetComponent<GearBehavior>();
            if (gb == null) continue;

            if (!gb.isFalling && gb.IsRotating)
                candidates.Add(gb);
        }

        if (candidates.Count > 0)
        {
            var pick = candidates[Random.Range(0, candidates.Count)];

            // KPI: caída aleatoria de engrane
            KPIPuzzle2Tracker.I?.RegisterRandomFall();

            pick.MakeFall();
        }

        // respeta cooldown fijo
        yield return new WaitForSeconds(randomFallCooldown);
        canTriggerRandomFall = true;
    }
}
