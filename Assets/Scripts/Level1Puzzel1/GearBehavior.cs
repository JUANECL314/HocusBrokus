using Photon.Pun;
using System.Collections;
using UnityEngine;

public class GearBehavior : MonoBehaviourPun
{
    private Renderer rend;
    private Rigidbody rb;

    // ---------- NUEVO -----------
    [Header("Cold Fall System")]
    public float coldFallProbability = 0.15f;
    public static bool oneColdGearAllowedToFall = false;
    private bool isHot = false;
    // -----------------------------

    [Header("Reactivation")]
    [SerializeField] private bool autoReactivateOnLand = true;
    public void SetAutoReactivateOnLand(bool v) => autoReactivateOnLand = v;

    [Header("State")]
    [SerializeField] private bool isRotating = false;
    public bool isFalling = false;
    private bool destroyedDoors = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    public bool IsRotating => isRotating;

    [SerializeField] private bool isShaking = false;

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

    void Start()
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
        isHot = false; // al inicio está frío
    }

    void Update()
    {
        if (isRotating)
            transform.Rotate(Vector3.forward * -rotationSpeed * Time.deltaTime, Space.Self);

        if (isFalling)
        {
            rb.isKinematic = false;
            rb.linearVelocity = new Vector3(0, -fallSpeed, 0);
        }
    }

    // --------------------------------------------------------
    // -------------- SISTEMA NUEVO DE CAÍDA FRÍA -------------
    // --------------------------------------------------------

    private void TryColdFall()
    {
        if (isHot) return; // no cae si está caliente
        if (isFalling || isShaking) return;

        if (oneColdGearAllowedToFall) return;

        float r = Random.value;
        if (r < coldFallProbability)
        {
            oneColdGearAllowedToFall = true;
            photonView.RPC("MakeFall", RpcTarget.All);
        }
    }

    // --------------------------------------------------------
    // ----------------------- OVERHEAT ------------------------
    // --------------------------------------------------------

    [PunRPC]
    public void StartRotation()
    {
        if (isRotating) return;
        isRotating = true;
        cooledDuringWindow = false;
        isHot = true; // ahora está caliente

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
        isHot = false;

        SoundManager.Instance?.StopLoop(LoopId);
        SoundManager.Instance?.Play(SfxKey.GearStop, transform);

        if (rotateFlowCo != null) { StopCoroutine(rotateFlowCo); rotateFlowCo = null; }
        if (overheatCo != null) { StopCoroutine(overheatCo); overheatCo = null; }
    }

    [PunRPC]
    private IEnumerator RotateAndChangeColorFlow()
    {
        rend.material.color = Color.gray;
        yield return new WaitForSeconds(0.5f);
        if (!isRotating) yield break;

        rend.material.color = new Color(1f, 0.5f, 0f);
        yield return new WaitForSeconds(0.5f);
        if (!isRotating) yield break;

        rend.material.color = Color.red;
    }

    [PunRPC]
    public void CoolDown()
    {
        rend.material.color = Color.gray;
        SoundManager.Instance?.Play(SfxKey.GearCoolHiss, transform);
        cooledDuringWindow = true;

        isHot = false;

        if (overheatCo != null) StopCoroutine(overheatCo);

        // al enfriarse → puede intentar caerse
        TryColdFall();

        StartCoroutine(RearmOverheatAfterDelay());
    }

    [PunRPC]
    public void ResetToInitialPosition(bool smooth = true)
    {
        isFalling = false;
        isShaking = false;

        oneColdGearAllowedToFall = false; // permite otro engranaje caer después

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
            StartCoroutine(ReturnToInitialPosition());
    }

    // -------------------------- FALL SYSTEM ----------------------------

    [PunRPC]
    public void MakeFall()
    {
        if (isFalling || isShaking) return;

        isRotating = false;
        isShaking = true;
        isHot = false;

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

        isShaking = false;

        yield return new WaitForSeconds(extraFallDelay);

        FallNow();
    }

    private void FallNow()
    {
        isFalling = true;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.constraints = RigidbodyConstraints.FreezeRotation;

        var puzzle = FindObjectOfType<ElementalPuzzle>();
        if (puzzle != null) puzzle.DoorPause(true);
    }

    // ----------------------- COLLISIONS -----------------------
    private void ReactivateAfterLand()
    {
        if (!isRotating && !isFalling && !isShaking)
            photonView.RPC("StartRotation", RpcTarget.All);
    }

    private IEnumerator RearmOverheatAfterDelay()
    {
        yield return new WaitForSeconds(overheatRearmSeconds);

        if (isRotating)
        {
            if (overheatCo != null) StopCoroutine(overheatCo);
            overheatCo = StartCoroutine(OverheatCountdown());
        }
    }

    private IEnumerator OverheatCountdown()
    {
        float elapsed = 0f;

        while (elapsed < overheatSeconds)
        {
            if (!isRotating)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // si terminó el conteo y NO se enfrió → se cae
        if (!cooledDuringWindow)
            photonView.RPC("MakeFall", RpcTarget.All);
    }


    [PunRPC]
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Water") && isRotating)
            photonView.RPC("CoolDown", RpcTarget.All);

        if (collision.gameObject.CompareTag("Earth") && isShaking)
        {
            photonView.RPC("RPC_ReturnToInitialPosition", RpcTarget.All);
            return;
        }

        if (collision.gameObject.CompareTag("Earth") && isFalling)
            photonView.RPC("RPC_ReturnToInitialPosition", RpcTarget.All);

        if (collision.gameObject.CompareTag("Ground") && isFalling)
        {
            photonView.RPC("RPC_DownForGood", RpcTarget.All);
        }
    }

    [PunRPC]
    public void RPC_DownForGood()
    {
        if (isShaking)
        {
            isShaking = false;
        }
        DownForGood();
    }

    public void DownForGood()
    {
        isFalling = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.None;
        SoundManager.Instance?.Play(SfxKey.GearStop, transform);

        oneColdGearAllowedToFall = false;
    }

    // ------------------------ RETURN --------------------------

    [PunRPC]
    public void RPC_ReturnToInitialPosition()
    {
        StartCoroutine(ReturnToInitialPosition());
    }

    [PunRPC]
    IEnumerator ReturnToInitialPosition()
    {
        isFalling = false;
        isShaking = false;

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

        oneColdGearAllowedToFall = false;
    }

    void OnDisable() { SoundManager.Instance?.StopLoop(LoopId); }
    void OnDestroy() { SoundManager.Instance?.StopLoop(LoopId); }
}
