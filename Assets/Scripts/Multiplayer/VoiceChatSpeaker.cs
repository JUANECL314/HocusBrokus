using UnityEngine;
using Photon.Voice.Unity;
using Photon.Voice.PUN;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PhotonVoiceView))]
public class VoiceChatSpeaker : MonoBehaviourPun
{
    private PhotonVoiceView voiceView;
    private Recorder recorder;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        voiceView = GetComponent<PhotonVoiceView>();      
        recorder = GetComponent<Recorder>();
        if(photonView.IsMine)
        {
            recorder.TransmitEnabled = true;
        }
        else
        {
            
            if(voiceView.SpeakerInUse != null)
            {
                var source = voiceView.SpeakerInUse.GetComponent<AudioSource>();
                if (source != null) {
                    source.spatialBlend = 1f;
                    source.rolloffMode = AudioRolloffMode.Linear;
                    source.maxDistance = 15f;
                }
            }
        }
    }

}
