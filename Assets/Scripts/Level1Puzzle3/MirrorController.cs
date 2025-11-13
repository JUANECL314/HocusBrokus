using System.Collections;
using UnityEngine;
using Photon.Pun;

public class MirrorController : MonoBehaviourPun
{
    [Header("Rotation")]
    public float rotateAngle = 15f;
    public Vector3 rotateAxis = Vector3.up;
    public float maxRotationOffset = 45f;

    [Header("Move Up")]
    public float defaultMoveAmount = 0.2f;
    public float maxUpOffset = 20f;
    public float revertMoveSpeed = 0.5f;

    [Header("Revert")]
    public float revertDelay = 0.5f;
    public float revertRotationSpeed = 60f;

    [Header("Alignment Target")]
    [HideInInspector] public Transform alignTarget; // dynamically assigned
    [SerializeField] private float alignSpeed = 2f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private float currentAngleOffset = 0f;
    private float currentUpOffset = 0f;
    private float lastInteractionTime = -Mathf.Infinity;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.localRotation;

        // Register with the puzzle manager if exists
        var manager = FindObjectOfType<MagicPillarPuzzleManager>();
        if (manager != null)
            manager.RegisterMirror(this);
    }

    void Update()
    {
        bool shouldRevert = (Time.time - lastInteractionTime) > revertDelay;

        if (shouldRevert && !Mathf.Approximately(currentAngleOffset, 0f))
        {
            currentAngleOffset = Mathf.MoveTowards(currentAngleOffset, 0f, revertRotationSpeed * Time.deltaTime);
            ApplyRotationOffset();
        }

        if (shouldRevert && !Mathf.Approximately(currentUpOffset, 0f))
        {
            currentUpOffset = Mathf.MoveTowards(currentUpOffset, 0f, revertMoveSpeed * Time.deltaTime);
            ApplyPositionOffset();
        }
    }

    // Mirror actions
    public void RotateLeft()  => photonView.RPC(nameof(RPC_RotateLeft), RpcTarget.AllBuffered);
    public void RotateRight() => photonView.RPC(nameof(RPC_RotateRight), RpcTarget.AllBuffered);
    public void MoveUp(float amount = 0f) => photonView.RPC(nameof(RPC_MoveUp), RpcTarget.AllBuffered, amount);

    [PunRPC]
    void RPC_RotateLeft()
    {
        currentAngleOffset = Mathf.Clamp(currentAngleOffset - rotateAngle, -Mathf.Abs(maxRotationOffset), Mathf.Abs(maxRotationOffset));
        ApplyRotationOffset();
        lastInteractionTime = Time.time;
        SoundManager.Instance?.Play(SfxKey.MirrorRotate, transform);
    }

    [PunRPC]
    void RPC_RotateRight()
    {
        currentAngleOffset = Mathf.Clamp(currentAngleOffset + rotateAngle, -Mathf.Abs(maxRotationOffset), Mathf.Abs(maxRotationOffset));
        ApplyRotationOffset();
        lastInteractionTime = Time.time;
        SoundManager.Instance?.Play(SfxKey.MirrorRotate, transform);
    }

    [PunRPC]
    void RPC_MoveUp(float amount)
    {
        if (amount <= 0f) amount = defaultMoveAmount;
        currentUpOffset = Mathf.Clamp(currentUpOffset + amount, 0f, Mathf.Abs(maxUpOffset));
        ApplyPositionOffset();
        lastInteractionTime = Time.time;
        SoundManager.Instance?.Play(SfxKey.MirrorMoveUp, transform);
    }

    // Puzzle manager trigger
    public void AlignToTarget()
    {
        photonView.RPC(nameof(RPC_AlignToTarget), RpcTarget.AllBuffered);
    }

    [PunRPC]
    void RPC_AlignToTarget()
    {
        StopAllCoroutines();

        if (alignTarget != null)
            StartCoroutine(SmoothAlign());
        else
        {
            // fallback: move fully up
            currentAngleOffset = 0f;
            currentUpOffset = maxUpOffset;
            ApplyRotationOffset();
            ApplyPositionOffset();
        }

        SoundManager.Instance?.Play(SfxKey.TargetIlluminate, transform);
    }

    private IEnumerator SmoothAlign()
    {
        float t = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime * alignSpeed;
            transform.position = Vector3.Lerp(startPos, alignTarget.position, t);
            transform.rotation = Quaternion.Slerp(startRot, alignTarget.rotation, t);
            yield return null;
        }

        transform.position = alignTarget.position;
        transform.rotation = alignTarget.rotation;
    }

    private void ApplyRotationOffset()
    {
        Vector3 axis = rotateAxis.normalized;
        Quaternion offset = Quaternion.AngleAxis(currentAngleOffset, axis);
        transform.localRotation = startRotation * offset;
    }

    private void ApplyPositionOffset()
    {
        transform.position = startPosition + Vector3.up * currentUpOffset;
    }

    public void ResetToStart()
    {
        currentAngleOffset = 0f;
        currentUpOffset = 0f;
        ApplyRotationOffset();
        ApplyPositionOffset();
        lastInteractionTime = -Mathf.Infinity;
    }
}
