// ...existing code...
using UnityEngine;

public class MirrorController : MonoBehaviour
{
    [Header("Rotation")]
    public float rotateAngle = 15f;            // degrees per press
    public Vector3 rotateAxis = Vector3.up;    // axis to rotate around
    [Tooltip("Maximum angle offset (degrees) from original rotation in either direction.")]
    public float maxRotationOffset = 45f;

    [Header("Move Up")]
    public float defaultMoveAmount = 0.2f;     // default upward step
    public float maxUpOffset = 20f;            // max distance above start position
    [Tooltip("Speed (units/sec) to move up/down when reverting.")]
    public float revertMoveSpeed = 0.5f;

    [Header("Revert")]
    [Tooltip("Seconds after last press before it starts reverting.")]
    public float revertDelay = 0.5f;
    [Tooltip("Degrees per second the mirror will rotate back to the original rotation.")]
    public float revertRotationSpeed = 60f;

    [Header("Mode")]
    [Tooltip("When true, rotation calls will instead move the object up (no rotation).")]
    public bool forceMoveUpMode = false;

    private Vector3 startPosition;
    private Quaternion startRotation;

    // current offsets from start
    private float currentAngleOffset = 0f;   // degrees
    private float currentUpOffset = 0f;      // units

    private float lastInteractionTime = -Mathf.Infinity;

    private bool isMovingSmoothly = false; // kept for compatibility (not using coroutine now)

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.localRotation;
    }

    void Update()
    {
        // If enough time has passed since last interaction, start reverting towards original state
        bool shouldRevert = (Time.time - lastInteractionTime) > revertDelay;

        // Revert rotation gradually if needed
        if (shouldRevert && !Mathf.Approximately(currentAngleOffset, 0f))
        {
            currentAngleOffset = Mathf.MoveTowards(currentAngleOffset, 0f, revertRotationSpeed * Time.deltaTime);
            ApplyRotationOffset();
        }

        // Revert vertical offset gradually if needed
        if (shouldRevert && !Mathf.Approximately(currentUpOffset, 0f))
        {
            currentUpOffset = Mathf.MoveTowards(currentUpOffset, 0f, revertMoveSpeed * Time.deltaTime);
            ApplyPositionOffset();
        }
    }

    public void RotateLeft()
    {
        if (forceMoveUpMode)
        {
            MoveUp();
            return;
        }

        // accumulate angle offset, clamp to maxRotationOffset
        currentAngleOffset = Mathf.Clamp(currentAngleOffset - rotateAngle, -Mathf.Abs(maxRotationOffset), Mathf.Abs(maxRotationOffset));
        ApplyRotationOffset();

        lastInteractionTime = Time.time;
    }

    public void RotateRight()
    {
        if (forceMoveUpMode)
        {
            MoveUp();
            return;
        }

        currentAngleOffset = Mathf.Clamp(currentAngleOffset + rotateAngle, -Mathf.Abs(maxRotationOffset), Mathf.Abs(maxRotationOffset));
        ApplyRotationOffset();

        lastInteractionTime = Time.time;
    }

    // Move up by amount (if amount <= 0, uses defaultMoveAmount).
    // This increments the currentUpOffset and clamps to maxUpOffset.
    public void MoveUp(float amount = 0f)
    {
        if (amount <= 0f) amount = defaultMoveAmount;

        currentUpOffset = Mathf.Clamp(currentUpOffset + amount, 0f, Mathf.Abs(maxUpOffset));
        ApplyPositionOffset();

        lastInteractionTime = Time.time;
    }

    private void ApplyRotationOffset()
    {
        // apply rotation relative to startRotation; ensure rotateAxis is normalized
        Vector3 axis = rotateAxis.normalized;
        Quaternion offset = Quaternion.AngleAxis(currentAngleOffset, axis);
        transform.localRotation = startRotation * offset;
    }

    private void ApplyPositionOffset()
    {
        transform.position = startPosition + Vector3.up * currentUpOffset;
    }

    // Optional: public helper to immediately reset (useful for editor/testing)
    public void ResetToStart()
    {
        currentAngleOffset = 0f;
        currentUpOffset = 0f;
        ApplyRotationOffset();
        ApplyPositionOffset();
        lastInteractionTime = -Mathf.Infinity;
    }

    private System.Collections.IEnumerator SmoothMove(Vector3 target, float duration)
    {
        // kept for compatibility but not used by default; MoveUp now changes offset and Update handles motion/revert
        isMovingSmoothly = true;
        Vector3 initial = transform.position;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(initial, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        transform.position = target;
        isMovingSmoothly = false;
    }
}
// ...existing code...