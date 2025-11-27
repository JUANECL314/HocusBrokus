using Photon.Pun;
using System.Collections;
using UnityEngine;

public class GearBehavior : MonoBehaviourPun
{
    private Renderer rend;
    private Rigidbody rb;

    // ------------------ STATE MACHINE ------------------

    private enum GearState
    {
        Idle,       // Quieto en su lugar
        Rotating,   // Girando normal
        Shaking,    // Temblando antes de caer
        Falling,    // Cayendo
        Returning   // Regresando a la posición inicial
    }

    [SerializeField] private GearState state = GearState.Idle;

    [Header("Reactivation")]
    [SerializeField] private bool autoReactivateOnLand = true;
    public void SetAutoReactivateOnLand(bool v) => autoReactivateOnLand = v;

    // Mantengo estos bools por compatibilidad con otros scripts
    [Header("Legacy State (read-only, para otros scripts)")]
    [SerializeField] private bool isRotating = false;
    [SerializeField] public bool isFalling = false;
    [SerializeField] private bool isShaking = false;
    public bool IsRotating => isRotating;

    [Header("Heat")]
    [SerializeField] private bool isHot = false;
    public bool IsHot => isHot;

    private bool destroyedDoors = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    [Header("Tuning")]
    public float rotationSpeed = 150f;
    public float timeToDestroyDoors = 20f;
    public float overheatSeconds = 15f;
    public float overheatRearmSeconds = 6f;
    public float fallSpeed = 2f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 1.3f;

    [Header("Shake Movement")]
    [SerializeField] private float shakeMoveOffsetX = 0.3f;

    [Header("Extra Delay Before Falling")]
    [SerializeField] private float extraFallDelay = 1.5f;

    private string LoopId => $"gear_loop_{GetInstanceID()}";

    private Coroutine rotateFlowCo;
    private Coroutine destroyDoorsCo;
    private Coroutine overheatCo;
    private bool cooledDuringWindow = false;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError($"{name} no tiene Rigidbody");
            return;
        }

        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.None;

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        rend.material.color = Color.gray;

        SetState(GearState.Idle);
    }

    void Update()
    {
        if (state == GearState.Rotating)
        {
            transform.Rotate(Vector3.forward * -rotationSpeed * Time.deltaTime, Space.Self);
        }
        else if (state == GearState.Falling)
        {
            rb.isKinematic = false;
            rb.linearVelocity = new Vector3(0, -fallSpeed, 0);
        }
    }

    // ---------------------------------------------------
    //  STATE HELPERS
    // ---------------------------------------------------

    private void SetState(GearState newState)
    {
        if (state == newState) return;

        state = newState;

        // Mantener los bools legacy sincronizados
        isRotating = (state == GearState.Rotating);
        isFalling = (state == GearState.Falling);
        isShaking = (state == GearState.Shaking);
    }

    // Helper para el puzzle
    public bool IsStableForDoor()
    {
        // "Estable" = girando, no cayendo ni temblando
        return state == GearState.Rotating;
    }

    // ---------------------------------------------------
    //  API PÚBLICA (Photon / otros scripts)
    // ---------------------------------------------------

    [PunRPC]
    public void StartRotation()
    {
        // Solo permitir pasar a Rotating desde Idle o Returning
        if (state == GearState.Rotating ||
            state == GearState.Shaking ||
            state == GearState.Falling)
            return;

        cooledDuringWindow = false;
        SetState(GearState.Rotating);

        SoundManager.Instance?.Play(SfxKey.GearStart, transform);
        SoundManager.Instance?.StartLoop(LoopId, SfxKey.GearLoop, transform);

        if (rotateFlowCo != null) StopCoroutine(rotateFlowCo);
        rotateFlowCo = StartCoroutine(RotateAndChangeColorFlow());

        if (overheatCo != null) StopCoroutine(overheatCo);
        overheatCo = StartCoroutine(OverheatCountdown());
    }

    [PunRPC]
    public void StopRotation()
    {
        if (state != GearState.Rotating) return;

        SetState(GearState.Idle);

        rend.material.color = Color.gray; // opcional, pero consistente
        isHot = false;                    // ❄️ NUEVO

        SoundManager.Instance?.StopLoop(LoopId);
        SoundManager.Instance?.Play(SfxKey.GearStop, transform);

        if (rotateFlowCo != null) { StopCoroutine(rotateFlowCo); rotateFlowCo = null; }
        if (overheatCo != null) { StopCoroutine(overheatCo); overheatCo = null; }
    }


    [PunRPC]
    private IEnumerator RotateAndChangeColorFlow()
    {
        // Frío
        rend.material.color = Color.gray;
        isHot = false;

        yield return new WaitForSeconds(0.5f);
        if (state != GearState.Rotating) yield break;

        // Tibio / naranja
        rend.material.color = new Color(1f, 0.5f, 0f);
        isHot = false;

        yield return new WaitForSeconds(0.5f);
        if (state != GearState.Rotating) yield break;

        // Rojo = caliente
        rend.material.color = Color.red;
        isHot = true;
    }


    [PunRPC]
    public void CoolDown()
    {
        // Solo tiene sentido si estaba girando (sobrecalentando)
        if (state != GearState.Rotating) return;

        rend.material.color = Color.gray;
        isHot = false;  // ❄️ NUEVO

        SoundManager.Instance?.Play(SfxKey.GearCoolHiss, transform);
        cooledDuringWindow = true;

        if (overheatCo != null) StopCoroutine(overheatCo);
        StartCoroutine(RearmOverheatAfterDelay());
    }


    [PunRPC]
    public void ResetToInitialPosition(bool smooth = true)
    {
        SetState(GearState.Idle);
        isHot = false;  
        rend.material.color = Color.gray;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;
        }

        if (!smooth)
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }
        else
        {
            StartCoroutine(ReturnToInitialPosition());
        }
    }

    [PunRPC]
    private IEnumerator OverheatCountdown()
    {
        cooledDuringWindow = false;
        float t = 0f;

        while (t < overheatSeconds)
        {
            if (state != GearState.Rotating) yield break;
            if (cooledDuringWindow) yield break;

            t += Time.deltaTime;
            yield return null;
        }

        Overheat();
    }

    [PunRPC]
    private IEnumerator RearmOverheatAfterDelay()
    {
        float t = 0f;

        while (t < overheatRearmSeconds)
        {
            if (state != GearState.Rotating) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        if (state == GearState.Rotating)
            overheatCo = StartCoroutine(OverheatCountdown());
    }

    [PunRPC]
    private void Overheat()
    {
        // Sobrecalentamiento cancela rotación y resetea el puzzle
        StopRotation();

        var puzzle = FindObjectOfType<ElementalPuzzle>();
        if (puzzle != null) puzzle.ResetFromOverheatAndReturnAll();
    }

    [PunRPC]
    public void ReactivateAfterLand()
    {
        if (state == GearState.Idle || state == GearState.Returning)
        {
            rend.material.color = Color.red;
            isHot = true;  
            SetState(GearState.Rotating);

            SoundManager.Instance?.Play(SfxKey.GearStart, transform);
            SoundManager.Instance?.StartLoop(LoopId, SfxKey.GearLoop, transform);

            if (overheatCo != null) StopCoroutine(overheatCo);
            overheatCo = StartCoroutine(OverheatCountdown());
        }
    }


    // ---------------------------------------------------------------------
    // ------------------------------  MAKEFALL -----------------------------
    // ---------------------------------------------------------------------

    [PunRPC]
    public void MakeFall()
    {
        // Solo dejar caer si estaba estable (Rotating)
        if (state != GearState.Rotating) return;

        // 🔥 NUEVO: si está caliente (rojo), NO se cae
        if (isHot)
            return;

        SetState(GearState.Shaking);

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;

        SoundManager.Instance?.StopLoop(LoopId);
        SoundManager.Instance?.Play(SfxKey.GearFall, transform);

        StartCoroutine(ShakeThenFall());
    }

    private IEnumerator ShakeThenFall()
    {
        float elapsed = 0f;
        Vector3 originalRot = transform.localEulerAngles;

        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(startPos.x + shakeMoveOffsetX, startPos.y, startPos.z);

        float moveDuration = shakeDuration;

        while (elapsed < moveDuration)
        {
            if (state != GearState.Shaking) yield break;

            float shake = Mathf.Sin(elapsed * 40f) * 5f;
            transform.localEulerAngles = new Vector3(
                originalRot.x + shake,
                originalRot.y,
                originalRot.z
            );

            float t = elapsed / moveDuration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Tiempo extra antes de caer
        yield return new WaitForSeconds(extraFallDelay);

        FallNow();
    }

    private void FallNow()
    {
        SetState(GearState.Falling);

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        var puzzle = FindObjectOfType<ElementalPuzzle>();
        if (puzzle != null) puzzle.DoorPause(true);
    }

    [PunRPC]
    void OnCollisionEnter(Collision collision)
    {
        // Agua enfría solo si estaba girando
        if (collision.gameObject.CompareTag("Water") && state == GearState.Rotating)
        {
            photonView.RPC("CoolDown", RpcTarget.All);
            return;
        }

        // Tierra cancela el temblor y regresa
        if (collision.gameObject.CompareTag("Earth") && state == GearState.Shaking)
        {
            StartCoroutine(ReturnToInitialPosition());
            return;
        }

        // Tierra pegando mientras cae → regresar
        if (collision.gameObject.CompareTag("Earth") && state == GearState.Falling)
        {
            StartCoroutine(ReturnToInitialPosition());
            return;
        }

        // Golpea piso normal
        if (collision.gameObject.CompareTag("Ground") && state == GearState.Falling)
        {
            SetState(GearState.Idle);

            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;

            SoundManager.Instance?.Play(SfxKey.GearStop, transform);
        }
    }

    [PunRPC]
    IEnumerator ReturnToInitialPosition()
    {
        SetState(GearState.Returning);

        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.None;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;
        float duration = 2f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, initialPosition, t);
            transform.rotation = Quaternion.Lerp(startRot, initialRotation, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = initialPosition;
        transform.rotation = initialRotation;

        // Al terminar de regresar, decidimos si reactivamos o se queda Idle
        if (autoReactivateOnLand)
            ReactivateAfterLand();
        else
            SetState(GearState.Idle);
    }

    void OnDisable() { SoundManager.Instance?.StopLoop(LoopId); }
    void OnDestroy() { SoundManager.Instance?.StopLoop(LoopId); }
}
