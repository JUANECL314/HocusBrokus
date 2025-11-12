using UnityEngine;
using Photon.Pun;
using Photon.Voice.PUN;
using Photon.Voice.Unity;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(PhotonView))]
public class MouthVoiceVisualizer : MonoBehaviourPun, IPunObservable
{
    [Header("Settings")]
    public float amplitudeThreshold = 0.015f; // Adjust for sensitivity
    public float smoothTime = 0.05f;          // Smaller = more reactive
    public bool debugInfo = false;

    private Renderer mouthRenderer;
    private Recorder recorder;
    private PhotonVoiceView voiceView;
    private PhotonView rootPhotonView;

    private float currentAmplitude = 0f;
    private float targetAmplitude = 0f;
    private bool isSpeakingRemote = false;

    void Awake()
    {
        mouthRenderer = GetComponent<Renderer>();
        mouthRenderer.enabled = false;

        // Walk up to the topmost transform in the hierarchy
        Transform t = transform;
        while (t.parent != null)
            t = t.parent;

        // Try to get references from the top root object
        rootPhotonView = t.GetComponentInChildren<PhotonView>();
        voiceView = t.GetComponentInChildren<PhotonVoiceView>();
        recorder = t.GetComponentInChildren<Recorder>();

        if (debugInfo)
        {
            Debug.Log($"[{name}] Found root: {t.name}");
            Debug.Log($"[{name}] PhotonVoiceView: {(voiceView ? "✅" : "❌")}, Recorder: {(recorder ? "✅" : "❌")}");
        }
    }

    void Update()
    {
        if (rootPhotonView == null) return;

        if (rootPhotonView.IsMine)
        {
            // Local: use mic amplitude
            if (recorder != null && recorder.LevelMeter != null)
                targetAmplitude = recorder.LevelMeter.CurrentAvgAmp;
            else
                targetAmplitude = 0f;

            currentAmplitude = Mathf.Lerp(currentAmplitude, targetAmplitude, Time.deltaTime / smoothTime);

            mouthRenderer.enabled = currentAmplitude > amplitudeThreshold;
        }
        else
        {
            // Remote: use synced bool
            mouthRenderer.enabled = isSpeakingRemote;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            bool isSpeaking = currentAmplitude > amplitudeThreshold;
            stream.SendNext(isSpeaking);
        }
        else
        {
            isSpeakingRemote = (bool)stream.ReceiveNext();
        }
    }
}
