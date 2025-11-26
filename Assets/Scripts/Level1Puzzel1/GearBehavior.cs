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

    [Header("State")]
    [SerializeField] private bool isRotating = false;
    public bool isFalling = false;
    private bool destroyedDoors = false;
    private Vector3 initialPosition;
    public bool IsRotating => isRotating;

    // Nuevo estado
    [SerializeField] private bool isShaking = false;

    [Header("Tuning")]
    public float rotationSpeed = 150f;
    public float timeToDestroyDoors = 20f;
    public float overheatSeconds = 15f;
    public float overheatRearmSeconds = 6f;
    public float fallSpeed = 2f;

    // Tiempo del temblor visible en inspector
    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 1.3f;

    // 🔥 Nuevo: cuánto se mueve en X durante el temblor
    [Header("Shake Movement")]
    [SerializeField] private float shakeMoveOffsetX = 0.3f;

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
        rend.material.color = Color.gray;
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

    [PunRPC]
    public void StartRotation()
    {
        if (isRotating) return;
        isRotating = true;
        cooledDuringWindow = false;

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
        if (overheatCo != null) StopCoroutine(overheatCo);
        StartCoroutine(RearmOverheatAfterDelay());
    }

    [PunRPC]
    public void ResetToInitialPosition(bool smooth = true)
    {
        isFalling = false;
        isShaking = false;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.None;
        }

        if (!smooth)
            transform.position = initialPosition;
        else
            StartCoroutine(ReturnToInitialPosition());
    }

    [PunRPC]
    private IEnumerator OverheatCountdown()
    {
        cooledDuringWindow = false;
        float t = 0f;

        while (t < overheatSeconds)
        {
            if (!isRotating) yield break;
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
            if (!isRotating) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        if (isRotating) overheatCo = StartCoroutine(OverheatCountdown());
    }

    [PunRPC]
    private void Overheat()
    {
        StopRotation();
        var puzzle = FindObjectOfType<ElementalPuzzle>();
        if (puzzle != null) puzzle.ResetFromOverheatAndReturnAll();
    }

    [PunRPC]
    public void ReactivateAfterLand()
    {
        if (!isRotating && !isFalling)
        {
            rend.material.color = Color.red;
            isRotating = true;
            SoundManager.Instance?.Play(SfxKey.GearStart, transform);
            SoundManager.Instance?.StartLoop(LoopId, SfxKey.GearLoop, transform);
        }
    }

    // ---------------------------------------------------------------------
    // ------------------------------  MAKEFALL -----------------------------
    // ---------------------------------------------------------------------

    [PunRPC]
    public void MakeFall()
    {
        if (isFalling || isShaking) return;

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

        // 🔥 Nuevo: movimiento pequeño en X configurable
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

    [PunRPC]
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Water") && isRotating)
            photonView.RPC("CoolDown",RpcTarget.All);

        if (collision.gameObject.CompareTag("Earth") && isShaking)
        {
            isShaking = false;
            StartCoroutine(ReturnToInitialPosition());
            return;
        }

        if (collision.gameObject.CompareTag("Earth") && isFalling)
            StartCoroutine(ReturnToInitialPosition());

        if (collision.gameObject.CompareTag("Ground") && isFalling)
        {
            isFalling = false;
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
        isFalling = false;
        isShaking = false;

        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.None;

        Vector3 start = transform.position;
        float elapsed = 0f;
        float duration = 2f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, initialPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = initialPosition;

        if (autoReactivateOnLand)
            ReactivateAfterLand();
    }

    void OnDisable() { SoundManager.Instance?.StopLoop(LoopId); }
    void OnDestroy() { SoundManager.Instance?.StopLoop(LoopId); }
}
