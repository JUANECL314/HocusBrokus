using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;

public class GearStateMachine : MonoBehaviourPun
{
    public GearEnumState currentState = GearEnumState.Init;

    private Renderer rend;
    private Rigidbody rb;

    [Header("Cold Fall System")]
    public float coldFallProbability = 0.15f;
    public static bool oneColdGearAllowedToFall = false;
    private bool isHot = false;
    private bool cooledDuringWindow = false;

    [Header("State")]
    [SerializeField] private bool isRotating = false;
    public bool isFalling = false;
    [SerializeField] private bool isShaking = false;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public bool IsRotating => isRotating;
    private string LoopId => $"gear_loop_{GetInstanceID()}";

    [Header("Tuning")]
    public float rotationSpeed = 150f;
    public float overheatSeconds = 15f;
    public float overheatRearmSeconds = 6f;
    public float fallSpeed = 2f;

    [Header("Shake Settings")]
    public float shakeDuration = 1.3f;
    public float shakeMoveOffsetX = 0.3f;
    public float extraFallDelay = 1.5f;

    private Coroutine rotateFlowCo;
    private Coroutine overheatCo;

    public enum GearEnumState
    {
        Init,
        Movement,
        OverHeat,
        Fall_Shake
    }

    // ----------------------------------------------------
    // INIT
    // ----------------------------------------------------
    void Start()
    {
        rend = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.None;

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        rend.material.color = Color.gray;

        StateMachineStatus(currentState);
    }

    // ----------------------------------------------------
    void Update()
    {
        if (isRotating && !isFalling)
            transform.Rotate(Vector3.forward * -rotationSpeed * Time.deltaTime, Space.Self);

        if (!isRotating && isFalling)
            rb.linearVelocity = new Vector3(0, -fallSpeed, 0);
    }

    // ----------------------------------------------------
    // STATE MACHINE
    // ----------------------------------------------------
    public void StateMachineStatus(GearEnumState next)
    {
        currentState = next;

        switch (next)
        {
            case GearEnumState.Init:
                SetupInitial();
                break;

            case GearEnumState.Movement:
                photonView.RPC("StartRotation", RpcTarget.All);
                break;

            case GearEnumState.OverHeat:
                if (overheatCo != null) StopCoroutine(overheatCo);
                overheatCo = StartCoroutine(OverheatCountdown());
                break;

            case GearEnumState.Fall_Shake:
                photonView.RPC("MakeFall", RpcTarget.All);
                break;
        }
    }

    void SetupInitial()
    {
        isRotating = false;
        isFalling = false;
        isShaking = false;
        isHot = false;

        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }

    // ----------------------------------------------------
    // RPC STATES
    // ----------------------------------------------------

    [PunRPC]
    public void StartRotation()
    {
        if (isRotating) return;

        isRotating = true;
        isFalling = false;
        cooledDuringWindow = false;
        isHot = true;

        SoundManager.Instance?.Play(SfxKey.GearStart, transform);
        SoundManager.Instance?.StartLoop(LoopId, SfxKey.GearLoop, transform);

        if (rotateFlowCo != null) StopCoroutine(rotateFlowCo);
        rotateFlowCo = StartCoroutine(RotateAndColor());

        StateMachineStatus(GearEnumState.OverHeat);
    }

    IEnumerator RotateAndColor()
    {
        rend.material.color = Color.gray;
        yield return new WaitForSeconds(0.5f);

        if (!isRotating) yield break;
        rend.material.color = new Color(1f, 0.5f, 0f);

        yield return new WaitForSeconds(0.5f);

        if (!isRotating) yield break;
        rend.material.color = Color.red;
    }

    IEnumerator OverheatCountdown()
    {
        float t = 0f;

        while (t < overheatSeconds)
        {
            if (!isRotating) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        if (!cooledDuringWindow)
            StateMachineStatus(GearEnumState.Fall_Shake);
    }

    [PunRPC]
    public void CoolDown()
    {
        rend.material.color = Color.gray;
        SoundManager.Instance?.Play(SfxKey.GearCoolHiss, transform);
        cooledDuringWindow = true;
        isHot = false;

        TryColdFall();

        StartCoroutine(RearmOverheat());
    }

    IEnumerator RearmOverheat()
    {
        yield return new WaitForSeconds(overheatRearmSeconds);

        if (isRotating)
            StateMachineStatus(GearEnumState.OverHeat);
    }

    void TryColdFall()
    {
        if (isHot) return;
        if (isShaking || isFalling) return;
        if (oneColdGearAllowedToFall) return;

        if (UnityEngine.Random.value < coldFallProbability)
        {
            oneColdGearAllowedToFall = true;
            StateMachineStatus(GearEnumState.Fall_Shake);
        }
    }

    // ----------------------------------------------------
    // FALL
    // ----------------------------------------------------
    [PunRPC]
    public void MakeFall()
    {
        if (isFalling || isShaking) return;

        isRotating = false;
        isShaking = true;
        isHot = false;

        rb.isKinematic = true;

        SoundManager.Instance?.StopLoop(LoopId);
        SoundManager.Instance?.Play(SfxKey.GearFall, transform);

        StartCoroutine(ShakeThenDrop());
    }

    IEnumerator ShakeThenDrop()
    {
        float t = 0f;
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.right * shakeMoveOffsetX;

        while (t < shakeDuration)
        {
            float shake = Mathf.Sin(t * 40f) * 5f;
            transform.localEulerAngles = new Vector3(shake, 0, 0);
            transform.position = Vector3.Lerp(start, end, t / shakeDuration);

            t += Time.deltaTime;
            yield return null;
        }

        isShaking = false;
        yield return new WaitForSeconds(extraFallDelay);

        isFalling = true;
        rb.isKinematic = false;
    }

    [PunRPC]
    public void RPC_ReturnToInitialPosition()
    {
        StartCoroutine(ReturnToInitial());
    }

    IEnumerator ReturnToInitial()
    {
        isFalling = false;
        isShaking = false;

        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.None;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float t = 0f;

        while (t < 2f)
        {
            float k = t / 2f;
            transform.position = Vector3.Lerp(startPos, initialPosition, k);
            transform.rotation = Quaternion.Lerp(startRot, initialRotation, k);

            t += Time.deltaTime;
            yield return null;
        }

        transform.position = initialPosition;
        transform.rotation = initialRotation;

        oneColdGearAllowedToFall = false;

        if (isRotating)
            StateMachineStatus(GearEnumState.Movement);
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.CompareTag("Water") && isRotating)
            photonView.RPC("CoolDown", RpcTarget.All);

        if (c.gameObject.CompareTag("Ground") && isFalling)
            photonView.RPC("RPC_ReturnToInitialPosition", RpcTarget.All);
    }

    private void OnDisable() { SoundManager.Instance?.StopLoop(LoopId); }
    private void OnDestroy() { SoundManager.Instance?.StopLoop(LoopId); }
}