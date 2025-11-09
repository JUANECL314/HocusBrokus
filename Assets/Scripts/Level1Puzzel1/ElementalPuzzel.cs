using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementalPuzzle : MonoBehaviour
{
    // --- Activador elemental ---
    private bool fireHit = false;
    private bool windHit = false;
    private bool waterHit = false;
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
            doorProgress += Time.deltaTime;
            if (doorProgress >= doorOpenTime)
            {
                OpenDoor();
                puzzleActivated = false;
            }
        }

        // Caidas aleatorias con gracia + cooldown
        if (puzzleActivated && canTriggerRandomFall && Time.time >= nextRandomFallAllowedTime)
            StartCoroutine(RandomFallTick());
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Fire") && !fireHit)
        {
            fireHit = true;
            SoundManager.Instance?.StartLoop(FireLoopId, SfxKey.FireIgniteLoop, transform);
        }

        if (other.CompareTag("Wind"))
            windHit = true;

        if (other.CompareTag("Water"))
            waterHit = true;

        // Activar engranajes con Fire + Wind (permitimos reactivar después de overheat)
        if (fireHit && windHit && !puzzleActivated)
        {
            // Si veniamos de overheat, simplemente rearmamos todo
            overheated = false;

            puzzleActivated = true;
            doorProgress = 0f;
            doorPaused = false;

            // 5 s de gracia antes de la primera caida random
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

            ScheduleActivateGears();
        }
    }

    // Pausar/reanudar puerta
    public void DoorPause(bool pause)
    {
        doorPaused = pause;
    }

    // Pausa la puerta y resetea TODOS los engranajes a su posición inicial.
    public void ResetFromOverheatAndReturnAll()
    {
        overheated = true;

        // limpiar flags de activacion (para obligar a relanzar Fuego+Viento)
        fireHit = false;
        windHit = false;
        waterHit = false;

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

    // estabilidad de los engranajes
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

    // Activacion segura
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

    // 🚪 Nuevo método modificado: ahora rota las puertas en lugar de destruirlas
    void OpenDoor()
    {
        // Inicia la rotación de las puertas en lugar de destruirlas
        StartCoroutine(RotateDoors());

        // Reset de estado del puzzle
        puzzleActivated = false;
        doorProgress = 0f;
        DoorPause(false);

        // (Opcional) parar caídas aleatorias
        canTriggerRandomFall = false;
    }

    // 🌀 Corutina para rotar puertas suavemente
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

    // Random fall con cooldown y gracia
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
            pick.MakeFall();
        }

        // respeta cooldown fijo
        yield return new WaitForSeconds(randomFallCooldown);
        canTriggerRandomFall = true;
    }
}
