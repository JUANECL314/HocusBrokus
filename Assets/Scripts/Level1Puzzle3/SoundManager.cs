using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public enum SfxKey
{
    MirrorRotate,
    MirrorMoveUp,
    LaserHitMirror,
    TargetSingleBlinkLoop,
    TargetHoldLoop,
    TargetSuccess
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
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1f;
        src.maxDistance = 25f;
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

    public void Play(SfxKey key, Vector3? worldPos = null, float pitch = 1f)
    {
        if (!_map.TryGetValue(key, out var e) || e.clip == null) return;

        var src = GetFreeSource();
        src.clip = e.clip;
        src.volume = e.volume;
        src.pitch = pitch;
        src.loop = false;
        src.spatialBlend = e.spatial ? e.spatialBlend : 0f;

        if (e.spatial && worldPos.HasValue) src.transform.position = worldPos.Value;
        else src.transform.localPosition = Vector3.zero;

        src.Play();
    }

    public void StartLoop(string loopId, SfxKey key, Vector3? worldPos = null)
    {
        if (_loops.ContainsKey(loopId)) return;
        if (!_map.TryGetValue(key, out var e) || e.clip == null || !e.loopable) return;

        var src = GetFreeSource();
        src.clip = e.clip;
        src.volume = e.volume;
        src.loop = true;
        src.spatialBlend = e.spatial ? e.spatialBlend : 0f;

        if (e.spatial && worldPos.HasValue) src.transform.position = worldPos.Value;
        else src.transform.localPosition = Vector3.zero;

        src.Play();
        _loops[loopId] = src;
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
