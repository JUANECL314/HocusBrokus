using UnityEngine;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
public class VoiceChat : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.V;
    private Recorder recorder;
    private bool isMuted = false;

    void Start()
    {
        // Obtiene el micrófono principal del jugador local
        recorder = GetComponentInChildren<Recorder>(true);

        if (recorder == null)
        {
            
            return;
        }

        // Activar voz por defecto
        recorder.TransmitEnabled = true;
        isMuted = false;
       
    }

    void Update()
    {
       

       
        if (Input.GetKeyDown(toggleKey))
        {
         
            isMuted = !isMuted;
            recorder.TransmitEnabled = !isMuted;
        }
    }
}
