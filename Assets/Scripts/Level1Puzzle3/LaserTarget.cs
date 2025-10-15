// ...existing code...
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserTarget : MonoBehaviour
{
    [Tooltip("How many distinct beams must be simultaneously hitting to trigger the success message.")]
    public int requiredBeams = 2;

    [Tooltip("How many seconds the required number of beams must continuously hit to trigger.")]
    public float requiredDuration = 5f;

    [Tooltip("Color when idle.")]
    public Color idleColor = Color.white;

    [Tooltip("Color to use for yellow state.")]
    public Color yellowColor = Color.yellow;

    [Tooltip("Blink interval (seconds) when exactly one beam is hitting.")]
    public float blinkInterval = 0.5f;

    [Tooltip("Blink interval (seconds) when required beams are hitting simultaneously (faster).")]
    public float fastBlinkInterval = 0.2f;

    // Emitted beam settings (the beam that comes out from the child "BeamSpawn")
    [Tooltip("Child transform name used as the beam spawn point (rotation adjustable).")]
    public string beamSpawnName = "BeamSpawn";
    [Tooltip("Max distance for the emitted beam.")]
    public float emittedBeamMaxDistance = 100f;
    [Tooltip("Layer mask for what the emitted beam can hit.")]
    public LayerMask emittedBeamLayers = ~0;
    [Tooltip("Width for the emitted beam LineRenderer.")]
    public float emittedBeamWidth = 0.02f;
    [Tooltip("Color for the emitted beam line (visual only).")]
    public Color emittedBeamColor = Color.yellow;

    private HashSet<int> activeBeams = new HashSet<int>();
    private float timer = 0f;
    private bool triggered = false;

    // Renderer support
    private Renderer meshRenderer;
    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private Coroutine blinkCoroutine;
    private bool permanentlyYellow = false;

    private enum BlinkMode { None, Single, FastMulti }
    private BlinkMode currentBlinkMode = BlinkMode.None;

    // Emitted beam runtime objects
    private Transform beamSpawn;
    private LineRenderer emittedLR;
    private bool muralCompleted = false;

    void Start()
    {
        // Try to find a Renderer first, then SpriteRenderer
        meshRenderer = GetComponent<Renderer>();
        if (meshRenderer == null)
            meshRenderer = GetComponentInChildren<Renderer>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Record base color (prefer sprite color if present)
        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;
        else if (meshRenderer != null)
            baseColor = meshRenderer.material.color;
        else
            baseColor = idleColor;

        // Ensure start color is baseColor (or idleColor if base wasn't set)
        SetColor(baseColor);

        // Find the BeamSpawn child
        var spawn = transform.Find(beamSpawnName);
        if (spawn == null)
        {
            // try any child named BeamSpawn (case sensitive) or first child named similarly
            foreach (Transform t in transform)
            {
                if (t.name == beamSpawnName)
                {
                    spawn = t;
                    break;
                }
            }
        }

        beamSpawn = spawn;
        if (beamSpawn != null)
        {
            Debug.Log($"LaserTarget: BeamSpawn found at '{beamSpawn.name}'. Emitted beam will be spawned from here when puzzle completes.");

            // If there's already a LineRenderer on the spawn, keep a reference but disable it
            emittedLR = beamSpawn.GetComponent<LineRenderer>();
            if (emittedLR != null)
            {
                // disable visuals at start so beam is not visible until puzzle completes
                emittedLR.enabled = false;
                emittedLR.positionCount = 0;
                Debug.Log("LaserTarget: Existing emitted LineRenderer found and disabled until puzzle completes.");
            }
            else
            {
                Debug.Log("LaserTarget: No emitted LineRenderer found — one will be created when puzzle completes.");
            }
        }
        else
        {
            Debug.LogWarning($"LaserTarget: child '{beamSpawnName}' not found — emitted beam will not be available.");
        }
    }

    public void Activate(int beamId)
    {
        if (activeBeams.Add(beamId))
        {
            Debug.Log($"LaserTarget: beam {beamId} started hitting. Count = {activeBeams.Count}");
        }
    }

    public void Deactivate(int beamId)
    {
        if (activeBeams.Remove(beamId))
        {
            Debug.Log($"LaserTarget: beam {beamId} stopped hitting. Count = {activeBeams.Count}");
        }
    }

    void Update()
    {
        if (permanentlyYellow)
        {
            // Once permanently yellow, keep the color and ignore other effects
            // Still update emitted beam visuals so it can interact with environment if needed
            UpdateEmittedBeam();
            return;
        }

        // Decide blink mode based on how many beams are hitting
        BlinkMode desiredMode = BlinkMode.None;
        if (!triggered)
        {
            if (activeBeams.Count == 1)
                desiredMode = BlinkMode.Single;
            else if (activeBeams.Count >= requiredBeams)
                desiredMode = BlinkMode.FastMulti;
            else
                desiredMode = BlinkMode.None;
        }

        // If blink mode changed, restart/stop coroutine accordingly
        if (desiredMode != currentBlinkMode)
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }

            currentBlinkMode = desiredMode;

            if (currentBlinkMode == BlinkMode.Single)
                blinkCoroutine = StartCoroutine(BlinkYellow(blinkInterval, BlinkMode.Single));
            else if (currentBlinkMode == BlinkMode.FastMulti)
                blinkCoroutine = StartCoroutine(BlinkYellow(fastBlinkInterval, BlinkMode.FastMulti));
            else
                SetColor(baseColor);
        }

        // Success timing logic: when required beams are held for requiredDuration, become permanently yellow
        if (activeBeams.Count >= requiredBeams)
        {
            timer += Time.deltaTime;
            if (timer >= requiredDuration && !triggered)
            {
                triggered = true;
                permanentlyYellow = true;

                // Stop blinking if running
                if (blinkCoroutine != null)
                {
                    StopCoroutine(blinkCoroutine);
                    blinkCoroutine = null;
                }
                currentBlinkMode = BlinkMode.None;

                // Set permanent yellow
                SetColor(yellowColor);

                OnRequiredBeamsHeld();
            }
        }
        else
        {
            timer = 0f;
            triggered = false;
        }

        // Update emitted beam visual and collision
        UpdateEmittedBeam();
    }

    private IEnumerator BlinkYellow(float interval, BlinkMode mode)
    {
        while (!permanentlyYellow && currentBlinkMode == mode &&
               ((mode == BlinkMode.Single && activeBeams.Count == 1) ||
                (mode == BlinkMode.FastMulti && activeBeams.Count >= requiredBeams)))
        {
            SetColor(yellowColor);
            yield return new WaitForSeconds(interval);
            if (permanentlyYellow || currentBlinkMode != mode) break;
            SetColor(baseColor);
            yield return new WaitForSeconds(interval);
        }

        // Ensure we restore base color if not permanently yellow
        if (!permanentlyYellow)
            SetColor(baseColor);

        blinkCoroutine = null;
        if (currentBlinkMode == mode)
            currentBlinkMode = BlinkMode.None;
    }

    private void UpdateEmittedBeam()
    {
        // Only draw/update the emitted beam if it exists (created/enabled after puzzle success)
        if (beamSpawn == null || emittedLR == null || !emittedLR.enabled) return;

        Vector3 origin = beamSpawn.position;
        Vector3 dir = beamSpawn.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, emittedBeamMaxDistance, emittedBeamLayers, QueryTriggerInteraction.Collide))
        {
            // Visual
            emittedLR.positionCount = 2;
            emittedLR.SetPosition(0, origin);
            emittedLR.SetPosition(1, hit.point);

            // If we hit a SunMural, color it yellow and log completion
            if (!muralCompleted && hit.collider != null && hit.collider.CompareTag("SunMural"))
            {
                SetObjectColor(hit.collider.gameObject, yellowColor);
                Debug.Log("Puzzle complete");
                muralCompleted = true;
            }
        }
        else
        {
            // No hit: draw full-length beam
            emittedLR.positionCount = 2;
            emittedLR.SetPosition(0, origin);
            emittedLR.SetPosition(1, origin + dir * emittedBeamMaxDistance);
        }
    }

    // Reusable small helper to color arbitrary GameObject (tries SpriteRenderer then Renderer)
    private void SetObjectColor(GameObject obj, Color c)
    {
        if (obj == null) return;

        var spr = obj.GetComponent<SpriteRenderer>();
        if (spr != null)
        {
            spr.color = c;
            return;
        }

        var rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            if (rend.material != null)
                rend.material.color = c;
        }
    }

    private void SetColor(Color c)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = c;
        }
        else if (meshRenderer != null)
        {
            // Use material instance to avoid changing shared material
            if (meshRenderer.material != null)
                meshRenderer.material.color = c;
        }
        else
        {
            // No renderer found; nothing to set
        }
    }

    // Create or enable the emitted beam when the puzzle successfully completes
    private void EnsureEmittedBeamCreated()
    {
        if (beamSpawn == null)
        {
            Debug.LogWarning("LaserTarget: cannot create emitted beam because BeamSpawn is missing.");
            return;
        }

        if (emittedLR == null)
        {
            emittedLR = beamSpawn.gameObject.AddComponent<LineRenderer>();
            emittedLR.material = new Material(Shader.Find("Unlit/Color"));
            Debug.Log("LaserTarget: Emitted LineRenderer created on BeamSpawn after puzzle completion.");
        }
        else
        {
            Debug.Log("LaserTarget: Enabling existing emitted LineRenderer after puzzle completion.");
        }

        emittedLR.startWidth = emittedBeamWidth;
        emittedLR.endWidth = emittedBeamWidth;
        emittedLR.positionCount = 0;
        emittedLR.startColor = emittedBeamColor;
        emittedLR.endColor = emittedBeamColor;
        emittedLR.useWorldSpace = true;
        emittedLR.enabled = true;
    }

    private void OnRequiredBeamsHeld()
    {
        Debug.Log($"LaserTarget: required {requiredBeams} beams held for {requiredDuration} seconds — SUCCESS!");
        // Create/enable emitted beam now that the success condition is met
        EnsureEmittedBeamCreated();
        // Add effects, sounds, etc.
    }
}
// ...existing code...