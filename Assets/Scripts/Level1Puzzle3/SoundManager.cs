using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum SfxKey
{
    MirrorRotate,
    MirrorMoveUp,
    LaserHitMirror,
    TargetFastPing,     
    TargetIlluminate    
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Mixer")]
    public AudioMixer mixer;                    // Asigna GameAudioMixer
    public AudioMixerGroup sfxGroup;            // Asigna el Group SFX
    [Tooltip("Nombre exacto del parámetro expuesto en Master")]
    public string masterVolParam = "MasterVol";
    [Tooltip("Nombre exacto del parámetro expuesto en SFX")]
    public string sfxVolParam = "SFXVol";

    [Header("Snapshots (opcional)")]
    public AudioMixerSnapshot normalSnapshot;   // Asigna "Normal" (si lo creaste)
    public AudioMixerSnapshot duckedSnapshot;   // Asigna "Ducked" (si lo creaste)
    public float duckFade = 0.15f;
    public float duckHold = 0.6f;

    [System.Serializable]
    public class SfxEntry
    {
        public SfxKey key;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool spatial = true;
        public bool loopable = false;
        [Range(0f, 1f)] public float spatialBlend = 1f;

        [Header("One-shot shaping")]
        [Tooltip("Si > 0, el one-shot se cortará tras este tiempo (segundos). Útil si tu clip es largo.")]
        public float maxOneShotDuration = -1f;

        [Tooltip("Rango aleatorio de pitch (min..max). Usa (1,1) para desactivar.")]
        public Vector2 pitchJitter = new Vector2(1f, 1f);
    }

    [Header("Clips")]
    public List<SfxEntry> clips = new List<SfxEntry>();

    [Header("Pool")]
    public int initialPool = 8;

    private readonly Dictionary<SfxKey, SfxEntry> _map = new();
    private readonly List<AudioSource> _pool = new();
    private readonly Dictionary<string, AudioSource> _loops = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        foreach (var e in clips)
            if (!_map.ContainsKey(e.key)) _map.Add(e.key, e);

        for (int i = 0; i < initialPool; i++)
            _pool.Add(CreateSource());

        // Cargar volúmenes previos
        if (mixer != null)
        {
            if (PlayerPrefs.HasKey(masterVolParam)) mixer.SetFloat(masterVolParam, PlayerPrefs.GetFloat(masterVolParam));
            if (PlayerPrefs.HasKey(sfxVolParam)) mixer.SetFloat(sfxVolParam, PlayerPrefs.GetFloat(sfxVolParam));
        }
    }

    AudioSource CreateSource()
    {
        var go = new GameObject("SFX_AudioSource");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        // Defaults 3D sensatos; se pueden sobrescribir por clip u objeto
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = 1.5f;
        src.maxDistance = 40f;
        src.dopplerLevel = 0f;

        if (sfxGroup != null) src.outputAudioMixerGroup = sfxGroup;
        return src;
    }

    AudioSource GetFreeSource()
    {
        foreach (var s in _pool) if (!s.isPlaying) return s;
        var extra = CreateSource();
        _pool.Add(extra);
        return extra;
    }

    // --- EXISTENTE: por posición (se mantiene) ---
    public void Play(SfxKey key, Vector3? worldPos = null, float pitch = 1f)
    {
        PlayInternal(key, worldPos, pitch, null);
    }

    public void StartLoop(string loopId, SfxKey key, Vector3? worldPos = null)
    {
        StartLoopInternal(loopId, key, worldPos, null);
    }

    // --- NUEVO: por Transform (aplica override por objeto si existe) ---
    public void Play(SfxKey key, Transform from, float pitch = 1f)
    {
        if (from == null) { Play(key, (Vector3?)null, pitch); return; }
        var area = from.GetComponent<SfxAreaOverride>();
        PlayInternal(key, from.position, pitch, area);
    }

    public void StartLoop(string loopId, SfxKey key, Transform from)
    {
        var area = from != null ? from.GetComponent<SfxAreaOverride>() : null;
        StartLoopInternal(loopId, key, from ? from.position : (Vector3?)null, area);
    }

    // --- Núcleo común ---
    void ApplyPitch(AudioSource src, SfxEntry e, float pitch)
    {
        src.pitch = (e.pitchJitter.x != 1f || e.pitchJitter.y != 1f)
            ? Random.Range(e.pitchJitter.x, e.pitchJitter.y)
            : pitch;
    }

    void Apply3D(AudioSource src, SfxEntry e, SfxAreaOverride area)
    {
        src.spatialBlend = e.spatial ? e.spatialBlend : 0f;
        if (area != null && area.enabledOverride)
        {
            src.spatialBlend = area.spatialBlend;
            src.rolloffMode = area.rolloff;
            src.minDistance = area.minDistance;
            src.maxDistance = area.maxDistance;
        }
    }

    void PlayInternal(SfxKey key, Vector3? worldPos, float pitch, SfxAreaOverride area)
    {
        if (!_map.TryGetValue(key, out var e))
        {
            Debug.LogWarning($"[SoundManager] No hay entrada de clip para key '{key}'. Agrega esta key en Clips.");
            return;
        }
        if (e.clip == null)
        {
            Debug.LogWarning($"[SoundManager] La key '{key}' no tiene AudioClip asignado.");
            return;
        }
        var src = GetFreeSource();
        src.clip = e.clip;
        src.volume = e.volume;
        src.loop = false;

        ApplyPitch(src, e, pitch);
        Apply3D(src, e, area);

        if (worldPos.HasValue) src.transform.position = worldPos.Value;
        else src.transform.localPosition = Vector3.zero;

        src.Play();

        if (e.maxOneShotDuration > 0f)
            StartCoroutine(StopAfter(src, e.maxOneShotDuration));
    }

    void StartLoopInternal(string loopId, SfxKey key, Vector3? worldPos, SfxAreaOverride area)
    {
        if (_loops.ContainsKey(loopId)) return;
        if (!_map.TryGetValue(key, out var e))
        {
            Debug.LogWarning($"[SoundManager] No hay entrada de clip para key '{key}'.");
            return;
        }
        if (e.clip == null || !e.loopable)
        {
            Debug.LogWarning($"[SoundManager] La key '{key}' {(e == null ? "no tiene entrada" : "no tiene clip o no es loopable")}.");
            return;
        }
        var src = GetFreeSource();
        src.clip = e.clip;
        src.volume = e.volume;
        src.loop = true;
        src.pitch = 1f;

        Apply3D(src, e, area);

        if (worldPos.HasValue) src.transform.position = worldPos.Value;
        else src.transform.localPosition = Vector3.zero;

        src.Play();
        _loops[loopId] = src;
    }

    IEnumerator StopAfter(AudioSource src, float seconds)
    {
        float t = 0f;
        while (t < seconds && src != null && src.isPlaying)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (src != null && src.isPlaying) src.Stop();
    }

    public void StopLoop(string loopId)
    {
        if (_loops.TryGetValue(loopId, out var src))
        {
            src.Stop();
            _loops.Remove(loopId);
        }
    }

    public void SetLoopPosition(string loopId, Vector3 worldPos)
    {
        if (_loops.TryGetValue(loopId, out var src))
            src.transform.position = worldPos;
    }

    // ---------- Volumen (0..1 lineal -> dB) ----------
    public void SetMasterVolume01(float v01) => SetExposedLinear(masterVolParam, v01);
    public void SetSfxVolume01(float v01) => SetExposedLinear(sfxVolParam, v01);

    void SetExposedLinear(string param, float v01)
    {
        if (mixer == null || string.IsNullOrEmpty(param)) return;
        v01 = Mathf.Clamp01(v01);
        float dB = (v01 > 0.0001f) ? Mathf.Log10(v01) * 20f : -80f;
        mixer.SetFloat(param, dB);
        PlayerPrefs.SetFloat(param, dB);
    }

    // ---------- Snapshots (opcional) ----------
    public void PunchSuccessDuck()
    {
        if (duckedSnapshot == null || normalSnapshot == null) return;
        duckedSnapshot.TransitionTo(duckFade);
        CancelInvoke(nameof(_BackToNormal));
        Invoke(nameof(_BackToNormal), duckFade + duckHold);
    }
    void _BackToNormal() => normalSnapshot.TransitionTo(duckFade);
}
