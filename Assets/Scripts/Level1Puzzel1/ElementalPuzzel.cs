using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class ElementalPuzzle : MonoBehaviourPun, IPunObservable
{
    // --- Activador elemental ---
    private bool fireHit = false;
    private bool windHit = false;

    [SerializeField] private bool puzzleActivated = false;
    [SerializeField] private bool overheated = false;  // Sobrecalentamiento
    private string FireLoopId => $"activator_fire_{GetInstanceID()}";

    // --- Puerta / progreso ---
    [Header("Door / Progress")]
    public float doorOpenTime = 8f;        // segundos necesarios en condiciones válidas
    [SerializeField] private float doorProgress = 0f;
    [SerializeField] private bool doorPaused = false;

    // Getters para UI
    public float Progress01 => Mathf.Clamp01(doorProgress / Mathf.Max(0.0001f, doorOpenTime));
    public bool IsActivated => puzzleActivated;
    public bool IsPaused => doorPaused;
    public bool IsOverheated => overheated;

    private bool _isActivating = false;
    private bool _activateScheduled = false;

    // --- Caídas aleatorias ---
    [Header("Random Falls")]
    public float randomFallCooldown = 5f;    // CD entre caídas
    public float randomFallGrace = 5f;       // tiempo extra al arrancar
    private bool canTriggerRandomFall = true;
    private float nextRandomFallAllowedTime = 0f;

    // ======================================================
    // UPDATE: solo el dueño del puzzle simula la lógica
    // ======================================================
    void Update()
    {
        // Si estamos en multiplayer, solo el dueño (MasterClient) simula
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        // Pausar/Reanudar puzzles según estabilidad de engranajes
        if (puzzleActivated)
        {
            bool stable = AllGearsStable();

            if (stable && doorPaused)
                DoorPause(false);
            else if (!stable && !doorPaused)
                DoorPause(true);
        }

        // Avanza progreso solo si está activo, no en pausa y todos los engranajes estables
        if (puzzleActivated && !doorPaused && AllGearsStable())
        {
            float dt = Time.deltaTime;
            doorProgress += dt;

            // KPI: tiempo estable acumulado (solo dueño)
            if (!PhotonNetwork.IsConnected || photonView.IsMine)
                KPIPuzzle2Tracker.I?.AccumulateStableTime(dt);

            if (doorProgress >= doorOpenTime)
            {
                OpenDoor();
                puzzleActivated = false;
            }
        }
        else if (puzzleActivated && doorPaused)
        {
            // KPI: tiempo en pausa (caos, solo dueño)
            if (!PhotonNetwork.IsConnected || photonView.IsMine)
                KPIPuzzle2Tracker.I?.AccumulatePausedTime(Time.deltaTime);
        }

        // Caídas aleatorias con gracia + cooldown (solo dueño)
        if (puzzleActivated && canTriggerRandomFall && Time.time >= nextRandomFallAllowedTime)
            StartCoroutine(RandomFallTick());
    }

    // ======================================================
    // TRIGGERS FIRE / WIND
    // ======================================================
    void OnTriggerEnter(Collider other)
    {
        // OFFLINE / sin Photon → lógica local
        if (!PhotonNetwork.IsConnected)
        {
            if (other.CompareTag("Fire"))
                TriggerElement("Fire");
            if (other.CompareTag("Wind"))
                TriggerElement("Wind");
            return;
        }

        // ONLINE: solo el dueño del puzzle procesa el trigger y lo replica
        if (!photonView.IsMine)
            return;

        if (other.CompareTag("Fire"))
            photonView.RPC("TriggerElement", RpcTarget.All, "Fire");

        if (other.CompareTag("Wind"))
            photonView.RPC("TriggerElement", RpcTarget.All, "Wind");
    }

    [PunRPC]
    void TriggerElement(string element)
    {
        if (element == "Fire" && !fireHit)
        {
            fireHit = true;
            SoundManager.Instance?.StartLoop(FireLoopId, SfxKey.FireIgniteLoop, transform);

            // KPI: primer impacto Fire (solo dueño)
            if (!PhotonNetwork.IsConnected || photonView.IsMine)
                KPIPuzzle2Tracker.I?.RegisterElementHit("Fire");
        }

        if (element == "Wind" && !windHit)
        {
            windHit = true;

            // KPI: primer impacto Wind (solo dueño)
            if (!PhotonNetwork.IsConnected || photonView.IsMine)
                KPIPuzzle2Tracker.I?.RegisterElementHit("Wind");
        }

        // Si ambos elementos están presentes, activa el puzzle
        if (fireHit && windHit && !puzzleActivated)
        {
            overheated = false;
            puzzleActivated = true;
            doorProgress = 0f;
            doorPaused = false;

            SoundManager.Instance?.Play(SfxKey.FireWindBoost, transform);
            SoundManager.Instance?.StopLoop(FireLoopId);

            var gears = GameObject.FindGameObjectsWithTag("Engranaje");
            foreach (var go in gears)
            {
                var gb = go.GetComponent<GearBehavior>();
                if (gb != null) gb.SetAutoReactivateOnLand(true);
            }

            // KPI: combinación Fire+Wind que activa el puzzle (solo dueño)
            if (!PhotonNetwork.IsConnected || photonView.IsMine)
                KPIPuzzle2Tracker.I?.RegisterPuzzleActivated();

            // Solo el dueño se encarga de caídas aleatorias y activación inicial
            if (!PhotonNetwork.IsConnected || photonView.IsMine)
            {
                nextRandomFallAllowedTime = Time.time + randomFallGrace;
                canTriggerRandomFall = true;
                ScheduleActivateGears();
            }
        }
    }

    // ======================================================
    // PAUSA / RESET
    // ======================================================
    public void DoorPause(bool pause)
    {
        doorPaused = pause;

        // KPI: evento de pausa sólo en el dueño
        if (pause && (!PhotonNetwork.IsConnected || photonView.IsMine))
            KPIPuzzle2Tracker.I?.RegisterDoorPause();
    }

    [PunRPC]
    public void ResetFromOverheatAndReturnAll()
    {
        overheated = true;

        // KPI: overheat global (solo dueño)
        if (!PhotonNetwork.IsConnected || photonView.IsMine)
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

    // ======================================================
    // ESTABILIDAD DE ENGRANAJES
    // ======================================================
    bool AllGearsStable()
    {
        var gears = GameObject.FindGameObjectsWithTag("Engranaje");
        if (gears.Length == 0) return false;

        foreach (var g in gears)
        {
            var gb = g.GetComponent<GearBehavior>();
            if (gb == null) return false;

            if (!gb.IsStableForDoor())
                return false;
        }
        return true;
    }

    // ======================================================
    // ACTIVAR ENGRANAJES
    // ======================================================
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
                if (gb != null && gb.photonView != null)
                {
                    // Arranca rotación en TODOS los clientes
                    gb.photonView.RPC("StartRotation", RpcTarget.All);
                }
            }
        }
        finally
        {
            _isActivating = false;
        }
    }

    void ScheduleActivateGears()
    {
        if (_activateScheduled) return;
        _activateScheduled = true;
        StartCoroutine(_ActivateNextFrame());
    }

    IEnumerator _ActivateNextFrame()
    {
        yield return null;
        _activateScheduled = false;
        ActivateGears();
    }

    // ======================================================
    // ABRIR PUERTA
    // ======================================================
    [PunRPC]
    void OpenDoor()
    {
        // KPI: puerta abierta (puzzle resuelto, sólo dueño)
        if (!PhotonNetwork.IsConnected || photonView.IsMine)
            KPIPuzzle2Tracker.I?.RegisterDoorOpened();

        // Inicia la rotación de las puertas en lugar de destruirlas
        StartCoroutine(RotateDoors());

        // Reset de estado del puzzle
        puzzleActivated = false;
        doorProgress = 0f;
        DoorPause(false);

        // parar caídas aleatorias
        canTriggerRandomFall = false;
    }

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

    // ======================================================
    // CAÍDAS ALEATORIAS (solo dueño elige, todos ven)
    // ======================================================
    IEnumerator RandomFallTick()
    {
        // Solo el dueño ejecuta esta corrutina en multiplayer
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            yield break;

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

            // KPI: caída aleatoria de engrane (solo dueño)
            if (!PhotonNetwork.IsConnected || photonView.IsMine)
                KPIPuzzle2Tracker.I?.RegisterRandomFall();

            // Hacerlo caer en TODOS los clientes
            if (pick.photonView != null)
                pick.photonView.RPC("MakeFall", RpcTarget.All);
        }

        // respeta cooldown fijo
        yield return new WaitForSeconds(randomFallCooldown);
        canTriggerRandomFall = true;
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Dueño del puzzle → envía estado
            stream.SendNext(doorProgress);
            stream.SendNext(puzzleActivated);
            stream.SendNext(doorPaused);
            stream.SendNext(overheated);
        }
        else
        {
            // Otros clientes → reciben estado
            doorProgress = (float)stream.ReceiveNext();
            puzzleActivated = (bool)stream.ReceiveNext();
            doorPaused = (bool)stream.ReceiveNext();
            overheated = (bool)stream.ReceiveNext();
        }
    }
}
