using Photon.Pun;
using System.Collections;
using UnityEngine;

public class GearBehavior : MonoBehaviourPun
{
    private Renderer rend;
    private Rigidbody rb;

    [Header("Reactivation")]
    [SerializeField] private bool autoReactivateOnLand = true;
    public void SetAutoReactivateOnLand(bool v) => autoReactivateOnLand = v;

    [Header("State (legacy flags)")]
    [SerializeField] private bool isRotating = false;
    public bool isFalling = false;
    [SerializeField] private bool isShaking = false;
    public bool IsRotating => isRotating;

    // 👇 NUEVO: helper para ElementalPuzzle
    public bool IsStableForDoor()
    {
        // Estable = está girando y no está temblando ni cayendo
        return isRotating && !isFalling && !isShaking;
    }

    [Header("Heat")]
    [SerializeField] private bool isHot = false;        // true = rojo, candidato a overheat
    public bool IsHot => isHot;

    // Solo un engranaje cayendo a la vez
    private static GearBehavior currentFallingGear = null;

    // Solo un engranaje sobrecalentándose a la vez
    private static GearBehavior currentOverheatingGear = null;

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

    private void Start()
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
        isRotating = false;
        isFalling = false;
        isShaking = false;
        isHot = false;
    }

    private void Update()
    {
        if (isRotating)
        {
            transform.Rotate(Vector3.forward * -rotationSpeed * Time.deltaTime, Space.Self);
        }
        else if (isFalling)
        {
            rb.isKinematic = false;
            rb.linearVelocity = new Vector3(0, -fallSpeed, 0);
        }
    }

    // ======================================================
    // ROTACIÓN / CALOR
    // ======================================================

    [PunRPC]
    public void StartRotation()
    {
        // No arrancar si ya está en caída/temblando
        if (isRotating || isFalling || isShaking)
            return;

        isRotating = true;

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
        if (!isRotating) return;

        isRotating = false;
        isHot = false; // se enfría

        if (currentOverheatingGear == this)
            currentOverheatingGear = null;

        SoundManager.Instance?.StopLoop(LoopId);
        SoundManager.Instance?.Play(SfxKey.GearStop, transform);

        if (rotateFlowCo != null) { StopCoroutine(rotateFlowCo); rotateFlowCo = null; }
        if (overheatCo != null) { StopCoroutine(overheatCo); overheatCo = null; }

        rend.material.color = Color.gray;
    }

    private IEnumerator RotateAndChangeColorFlow()
    {
        // Transición progresiva:
        // gris → naranja/amarillo → rojo
        Color c0 = Color.gray;
        Color c1 = new Color(1f, 0.75f, 0f); // amarillo/naranja
        Color c2 = Color.red;

        float dur1 = 1.0f; // tiempo de gris→naranja
        float dur2 = 1.0f; // tiempo de naranja→rojo

        isHot = false;
        float t = 0f;

        // Fase 1: gris → naranja
        while (t < dur1)
        {
            if (!isRotating) yield break;

            float k = t / dur1;
            rend.material.color = Color.Lerp(c0, c1, k);

            t += Time.deltaTime;
            yield return null;
        }

        // Fase 2: naranja → rojo
        t = 0f;
        while (t < dur2)
        {
            if (!isRotating) yield break;

            float k = t / dur2;
            rend.material.color = Color.Lerp(c1, c2, k);

            t += Time.deltaTime;
            yield return null;
        }

        // Estado final: rojo caliente
        if (isRotating)
        {
            rend.material.color = c2;
            isHot = true;
        }
    }

    [PunRPC]
    public void CoolDown()
    {
        // Enfría solo si está girando
        if (!isRotating) return;

        rend.material.color = Color.gray;
        isHot = false;

        SoundManager.Instance?.Play(SfxKey.GearCoolHiss, transform);
        cooledDuringWindow = true;

        if (overheatCo != null) StopCoroutine(overheatCo);
        StartCoroutine(RearmOverheatAfterDelay());
    }

    [PunRPC]
    public void ResetToInitialPosition(bool smooth = true)
    {
        if (currentFallingGear == this)
            currentFallingGear = null;
        if (currentOverheatingGear == this)
            currentOverheatingGear = null;

        isRotating = false;
        isFalling = false;
        isShaking = false;
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

    private IEnumerator OverheatCountdown()
    {
        // 1) Esperar a que realmente esté rojo y en estado estable
        while (true)
        {
            if (!isRotating) yield break;
            if (cooledDuringWindow) yield break;

            // Solo arrancamos contador de overheat si:
            //  - está rojo (isHot)
            //  - no está cayendo ni temblando
            if (isHot && !isFalling && !isShaking)
                break;

            yield return null;
        }

        cooledDuringWindow = false;
        float t = 0f;

        // 2) Solo un engranaje en proceso de sobrecalentamiento a la vez
        if (currentOverheatingGear != null && currentOverheatingGear != this)
            yield break;

        currentOverheatingGear = this;

        // 3) Contar tiempo de sobrecalentamiento visible
        while (t < overheatSeconds)
        {
            if (!isRotating || !isHot || isFalling || isShaking)
            {
                if (currentOverheatingGear == this)
                    currentOverheatingGear = null;
                yield break;
            }

            if (cooledDuringWindow)
            {
                if (currentOverheatingGear == this)
                    currentOverheatingGear = null;
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        Overheat();
    }

    private IEnumerator RearmOverheatAfterDelay()
    {
        float t = 0f;

        while (t < overheatRearmSeconds)
        {
            if (!isRotating) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        if (isRotating)
            overheatCo = StartCoroutine(OverheatCountdown());
    }

    [PunRPC]
    private void Overheat()
    {
        // Solo sobrecalentar si:
        //  - sigue girando
        //  - sigue rojo
        //  - no está cayendo / temblando
        //  - es el engranaje "dueño" del slot de overheat
        if (!isRotating || !isHot || isFalling || isShaking || currentOverheatingGear != this)
        {
            if (currentOverheatingGear == this)
                currentOverheatingGear = null;
            return;
        }

        Debug.Log($"[GearBehavior] OVERHEAT en {name}");

        isHot = false;
        StopRotation();

        if (currentOverheatingGear == this)
            currentOverheatingGear = null;

        var puzzle = FindObjectOfType<ElementalPuzzle>();
        if (puzzle != null)
            puzzle.ResetFromOverheatAndReturnAll();
    }

    // ======================================================
    // CAÍDA
    // ======================================================

    [PunRPC]
    public void MakeFall()
    {
        // No hacer nada si ya está cayendo/temblando
        if (isFalling || isShaking) return;

        // Solo caer si estaba girando
        if (!isRotating) return;

        // Solo 1 engranaje cayendo a la vez
        if (currentFallingGear != null && currentFallingGear != this)
            return;

        // Si está rojo/caliente no se cae
        if (isHot)
            return;

        currentFallingGear = this;

        isRotating = false;
        isShaking = true;

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
            if (!isShaking) yield break;

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
        isShaking = false;
        isFalling = true;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        var puzzle = FindObjectOfType<ElementalPuzzle>();
        if (puzzle != null) puzzle.DoorPause(true);
    }

    // ======================================================
    // COLISIONES
    // ======================================================

    [PunRPC]
    private void OnCollisionEnter(Collision collision)
    {
        // Agua enfría si está girando
        if (collision.gameObject.CompareTag("Water") && isRotating)
        {
            photonView.RPC("CoolDown", RpcTarget.All);
            return;
        }

        // Tierra cancela el temblor y regresa
        if (collision.gameObject.CompareTag("Earth") && isShaking)
        {
            isShaking = false;
            StartCoroutine(ReturnToInitialPosition());
            return;
        }

        // Tierra pegando mientras cae → regresar
        if (collision.gameObject.CompareTag("Earth") && isFalling)
        {
            isFalling = false;
            StartCoroutine(ReturnToInitialPosition());
            return;
        }

        // Golpea piso normal
        if (collision.gameObject.CompareTag("Ground") && isFalling)
        {
            isFalling = false;

            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;

            SoundManager.Instance?.Play(SfxKey.GearStop, transform);

            if (currentFallingGear == this)
                currentFallingGear = null;
        }
    }

    [PunRPC]
    private IEnumerator ReturnToInitialPosition()
    {
        isFalling = false;
        isShaking = false;

        if (currentFallingGear == this)
            currentFallingGear = null;
        if (currentOverheatingGear == this)
            currentOverheatingGear = null;

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

        if (autoReactivateOnLand)
            ReactivateAfterLand();
    }

    [PunRPC]
    public void ReactivateAfterLand()
    {
        if (!isRotating && !isFalling && !isShaking)
        {
            rend.material.color = Color.red;
            isHot = true;

            isRotating = true;

            SoundManager.Instance?.Play(SfxKey.GearStart, transform);
            SoundManager.Instance?.StartLoop(LoopId, SfxKey.GearLoop, transform);

            if (overheatCo != null) StopCoroutine(overheatCo);
            overheatCo = StartCoroutine(OverheatCountdown());
        }
    }

    private void OnDisable()
    {
        SoundManager.Instance?.StopLoop(LoopId);
        if (currentFallingGear == this)
            currentFallingGear = null;
        if (currentOverheatingGear == this)
            currentOverheatingGear = null;
    }

    private void OnDestroy()
    {
        SoundManager.Instance?.StopLoop(LoopId);
        if (currentFallingGear == this)
            currentFallingGear = null;
        if (currentOverheatingGear == this)
            currentOverheatingGear = null;
    }
}
