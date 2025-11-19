using UnityEngine;
using System.Collections;

public class PlayerVortexReceiver : MonoBehaviour
{
    private Rigidbody rb;
    private FreeFlyCameraMulti movement;
    private Camera playerCam;

    private Coroutine vortexRoutine;
    private Coroutine shakeRoutine;

    [Header("Camera Spin Settings")]
    public float spinSpeed = 300f;   // degrees per second at max
    public float spinDurationScale = 1f;

    [Header("Screen Shake")]
    public float shakeIntensity = 0.1f;
    public float shakeFrequency = 25f;

    [Header("Player Spin")]
    public float playerSpinSpeed = 500f;  // how fast the PLAYER rotates
    public float spinDrag = 0.97f;

    [Header("FOV Distortion")]
    public float fovIncrease = 25f;
    public float fovSmooth = 7f;

    [Header("Respawn")]
    public PlayerRespawn respawn; // optional, will be auto-found in Start if null

    private float originalFOV;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<FreeFlyCameraMulti>();
    }

    private void Start()
    {
        if (movement && movement.cameraTransform)
        {
            playerCam = movement.cameraTransform.GetComponent<Camera>();
            if (playerCam)
                originalFOV = playerCam.fieldOfView;
        }

        if (respawn == null)
            respawn = GetComponent<PlayerRespawn>();
    }

    public void StartVortexPush(Transform vortex, float duration, float forward, float up, float horizontal)
    {
        if (vortexRoutine != null)
            StopCoroutine(vortexRoutine);

        vortexRoutine = StartCoroutine(VortexPushRoutine(
            vortex, duration, forward, up, horizontal));
    }

    private IEnumerator VortexPushRoutine(Transform vortex, float duration,
                                          float forward, float up, float horizontal)
    {
        // Freeze player controls
        if (movement != null)
            movement.SetFrozen(true);

        // disable gravity while vortex acts
        rb.useGravity = false;

        // clear velocities
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float t = 0f;

        // Start camera shake
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ScreenShake(duration));

        float playerSpinVelocity = 0f;

        while (t < duration)
        {
            t += Time.fixedDeltaTime;

            float curve = t / duration;
            curve = curve * curve * curve; // smooth ramp

            // FORCE ---------------------------------------
            Vector3 push =
                (vortex.forward * forward * curve) +
                (Vector3.up * up * curve) +
                (vortex.right * horizontal * curve);

            rb.AddForce(push, ForceMode.Acceleration);


            // CAMERA SPIN ----------------------------------
            if (movement != null && movement.cameraTransform != null)
            {
                float spinAmount = spinSpeed * curve * spinDurationScale * Time.fixedDeltaTime;
                Vector3 rot = movement.cameraTransform.localEulerAngles;
                rot.z += spinAmount;
                movement.cameraTransform.localEulerAngles = rot;
            }


            // PLAYER SPIN ----------------------------------
            playerSpinVelocity += playerSpinSpeed * curve * Time.fixedDeltaTime;
            playerSpinVelocity *= spinDrag;

            transform.Rotate(0, playerSpinVelocity * Time.fixedDeltaTime, 0, Space.World);


            // FOV DISTORTION -------------------------------
            if (playerCam != null)
            {
                float targetFOV = originalFOV + fovIncrease * curve;
                // note: use Time.deltaTime here because FOV is frame-based (visual)
                playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, targetFOV, Time.deltaTime * fovSmooth);
            }

            yield return new WaitForFixedUpdate();
        }


        // ---------------------
        // CLEANUP BEFORE RESPAWN
        // ---------------------

        // Stop shake coroutine
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        // Reset camera Z tilt
        if (movement != null && movement.cameraTransform != null)
        {
            Vector3 rot = movement.cameraTransform.localEulerAngles;
            rot.z = 0f;
            movement.cameraTransform.localEulerAngles = rot;
        }

        // Reset FOV (snap back or Lerp back quickly)
        if (playerCam != null)
            playerCam.fieldOfView = originalFOV;

        // Ensure Rigidbody is stable before respawn
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // ---------------------
        // AUTO-RESPAWN (blocking)
        // ---------------------
        if (respawn != null)
        {
            // ForceRespawn() should call your existing TeleportToSpawn() logic.
            // This should be synchronous (immediate teleport).
            respawn.ForceRespawn();
        }
        else
        {
            Debug.LogWarning("[PlayerVortexReceiver] No PlayerRespawn found — skipping auto-respawn.");
        }

        // small yield to let teleport settle (one frame)
        yield return null;

        // ensure gravity back on and velocities cleared
        rb.useGravity = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Restore movement control AFTER respawn
        if (movement != null)
            movement.SetFrozen(false);

        vortexRoutine = null;
    }

    // ======================================
    // SCREEN SHAKE
    // ======================================

    private IEnumerator ScreenShake(float duration)
    {
        if (movement == null || movement.cameraTransform == null)
            yield break;

        Transform cam = movement.cameraTransform;
        Vector3 originalPos = cam.localPosition;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            float curve = t / duration;

            Vector3 offset = new Vector3(
                Mathf.Sin(Time.time * shakeFrequency) * shakeIntensity * (curve * 1.3f),
                Mathf.Cos(Time.time * shakeFrequency * 1.2f) * shakeIntensity * (curve * 1.3f),
                0
            );

            cam.localPosition = movement.cameraOffset + offset;

            yield return null;
        }

        cam.localPosition = movement.cameraOffset;
    }
}
