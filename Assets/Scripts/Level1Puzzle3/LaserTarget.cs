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

    [Tooltip("Glow color when active or triggered.")]
    public Color glowColor = Color.yellow;

    [Tooltip("Blink interval (seconds) when exactly one beam is hitting.")]
    public float blinkInterval = 0.5f;

    [Tooltip("Blink interval (seconds) when required beams are hitting simultaneously (faster).")]
    public float fastBlinkInterval = 0.2f;

    [Header("Glow Settings")]
    public float maxGlowIntensity = 3f;
    public float glowPulseSpeed = 2f;

    [Header("Emitted Beam Settings")]
    public string beamSpawnName = "BeamSpawn";
    public float emittedBeamMaxDistance = 100f;
    public LayerMask emittedBeamLayers = ~0;
    public float emittedBeamWidth = 0.02f;
    public Color emittedBeamColor = Color.yellow;

    private HashSet<int> activeBeams = new HashSet<int>();
    private float timer = 0f;
    private bool triggered = false;

    private Renderer meshRenderer;
    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private Coroutine blinkCoroutine;
    private bool permanentlyGlowing = false;

    private enum BlinkMode { None, Single, FastMulti }
    private BlinkMode currentBlinkMode = BlinkMode.None;

    private Transform beamSpawn;
    private LineRenderer emittedLR;
    private bool muralCompleted = false;

    void Start()
    {
        meshRenderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

        baseColor = spriteRenderer ? spriteRenderer.color :
                    (meshRenderer && meshRenderer.material ? meshRenderer.material.color : idleColor);

        SetColor(baseColor);
        DisableEmission();

        beamSpawn = transform.Find(beamSpawnName);
        if (beamSpawn == null)
        {
            foreach (Transform t in transform)
                if (t.name == beamSpawnName) { beamSpawn = t; break; }
        }

        if (beamSpawn != null)
        {
            emittedLR = beamSpawn.GetComponent<LineRenderer>();
            if (emittedLR != null)
            {
                emittedLR.enabled = false;
                emittedLR.positionCount = 0;
            }
        }
    }

    public void Activate(int beamId)
    {
        if (activeBeams.Add(beamId))
            Debug.Log($"LaserTarget: beam {beamId} started hitting. Count = {activeBeams.Count}");
    }

    public void Deactivate(int beamId)
    {
        if (activeBeams.Remove(beamId))
            Debug.Log($"LaserTarget: beam {beamId} stopped hitting. Count = {activeBeams.Count}");
    }

    void Update()
    {
        if (permanentlyGlowing)
        {
            UpdateGlowEffect();
            UpdateEmittedBeam();
            return;
        }

        BlinkMode desiredMode = BlinkMode.None;
        if (activeBeams.Count == 1) desiredMode = BlinkMode.Single;
        else if (activeBeams.Count >= requiredBeams) desiredMode = BlinkMode.FastMulti;

        if (desiredMode != currentBlinkMode)
        {
            if (blinkCoroutine != null) { StopCoroutine(blinkCoroutine); blinkCoroutine = null; }
            currentBlinkMode = desiredMode;

            if (currentBlinkMode == BlinkMode.Single)
                blinkCoroutine = StartCoroutine(BlinkGlow(blinkInterval, BlinkMode.Single));
            else if (currentBlinkMode == BlinkMode.FastMulti)
                blinkCoroutine = StartCoroutine(BlinkGlow(fastBlinkInterval, BlinkMode.FastMulti));
            else
                ResetGlow();
        }

        if (activeBeams.Count >= requiredBeams)
        {
            timer += Time.deltaTime;
            if (timer >= requiredDuration && !triggered)
            {
                triggered = true;
                permanentlyGlowing = true;

                if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
                currentBlinkMode = BlinkMode.None;

                EnableGlow(true);
                OnRequiredBeamsHeld();
            }
        }
        else
        {
            timer = 0f;
            triggered = false;
        }

        if (activeBeams.Count > 0 || permanentlyGlowing)
            UpdateGlowEffect();
        else
            DisableEmission();

        UpdateEmittedBeam();
    }

    private IEnumerator BlinkGlow(float interval, BlinkMode mode)
    {
        while (!permanentlyGlowing && currentBlinkMode == mode &&
               ((mode == BlinkMode.Single && activeBeams.Count == 1) ||
                (mode == BlinkMode.FastMulti && activeBeams.Count >= requiredBeams)))
        {
            if (permanentlyGlowing || currentBlinkMode != mode) break;

            EnableGlow(true);
            PlayBlinkPing(mode);
            yield return new WaitForSeconds(interval);

            if (permanentlyGlowing || currentBlinkMode != mode) break;

            EnableGlow(false);
            PlayBlinkPing(mode);
            yield return new WaitForSeconds(interval);
        }

        if (!permanentlyGlowing) ResetGlow();
        blinkCoroutine = null;
        if (currentBlinkMode == mode) currentBlinkMode = BlinkMode.None;
    }

    private void EnableGlow(bool state)
    {
        if (meshRenderer == null || meshRenderer.material == null) return;
        if (state)
        {
            meshRenderer.material.EnableKeyword("_EMISSION");
            meshRenderer.material.SetColor("_EmissionColor", glowColor * maxGlowIntensity);
        }
        else
        {
            DisableEmission();
        }
    }

    private void DisableEmission()
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.DisableKeyword("_EMISSION");
            meshRenderer.material.SetColor("_EmissionColor", Color.black);
        }
    }

    private void ResetGlow()
    {
        DisableEmission();
        SetColor(baseColor);
    }

    private void UpdateGlowEffect()
    {
        if (meshRenderer == null || meshRenderer.material == null) return;
        if (!meshRenderer.material.IsKeywordEnabled("_EMISSION")) return;

        float pulse = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) / 2f;
        float intensity = Mathf.Lerp(0.5f, maxGlowIntensity, pulse);
        meshRenderer.material.SetColor("_EmissionColor", glowColor * intensity);
    }

    private void UpdateEmittedBeam()
    {
        if (beamSpawn == null || emittedLR == null || !emittedLR.enabled) return;

        Vector3 origin = beamSpawn.position;
        Vector3 dir = beamSpawn.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, emittedBeamMaxDistance, emittedBeamLayers))
        {
            emittedLR.positionCount = 2;
            emittedLR.SetPosition(0, origin);
            emittedLR.SetPosition(1, hit.point);

            if (!muralCompleted && hit.collider != null && hit.collider.CompareTag("SunMural"))
            {
                var mural = hit.collider.GetComponent<SunMuralQuad>();
                if (mural != null)
                    mural.Show(); // make the quad visible when hit
                muralCompleted = true;
            }

        }
        else
        {
            emittedLR.positionCount = 2;
            emittedLR.SetPosition(0, origin);
            emittedLR.SetPosition(1, origin + dir * emittedBeamMaxDistance);
        }
    }

    private void SetObjectColor(GameObject obj, Color c)
    {
        if (obj == null) return;
        var spr = obj.GetComponent<SpriteRenderer>();
        if (spr != null) spr.color = c;
        else
        {
            var rend = obj.GetComponent<Renderer>();
            if (rend != null && rend.material != null)
                rend.material.color = c;
        }
    }

    private void SetColor(Color c)
    {
        if (spriteRenderer != null) spriteRenderer.color = c;
        if (meshRenderer != null && meshRenderer.material != null)
            meshRenderer.material.color = c;
    }

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
        }

        emittedLR.startWidth = emittedBeamWidth;
        emittedLR.endWidth = emittedBeamWidth;
        emittedLR.positionCount = 0;
        emittedLR.startColor = emittedBeamColor;
        emittedLR.endColor = emittedBeamColor;

        if (emittedLR.material != null)
            emittedLR.material.color = emittedBeamColor;

        emittedLR.useWorldSpace = true;
        emittedLR.enabled = true;
    }

    void PlayBlinkPing(BlinkMode mode)
    {
        if (mode == BlinkMode.Single)
            SoundManager.Instance?.Play(SfxKey.LaserHitMirror, transform);
        else if (mode == BlinkMode.FastMulti)
            SoundManager.Instance?.Play(SfxKey.TargetFastPing, transform);
    }

    private void OnRequiredBeamsHeld()
    {
        Debug.Log($"LaserTarget: required {requiredBeams} beams held for {requiredDuration} seconds — SUCCESS!");
        EnsureEmittedBeamCreated();

        // One-shot al pasar a brillo permanente
        SoundManager.Instance?.Play(SfxKey.TargetIlluminate, transform.position);

        // ⬇️ KPI: cerrar medición al completar el puzzle
        KPITracker.Instance?.OnPuzzleCompleted();
    }
}
