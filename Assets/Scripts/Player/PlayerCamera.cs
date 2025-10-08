using UnityEngine;
using Photon.Pun;
[RequireComponent(typeof(PhotonView))]
public class PlayerCamera : MonoBehaviourPun
{
    public Camera playerCamera;
    public AudioListener audioListener;
    
    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>(true);
        audioListener = GetComponentInChildren<AudioListener>(true);
        if (photonView.IsMine)
        {
            
            
            playerCamera.enabled = true;
            audioListener.enabled = true;
        }
        else
        {
            playerCamera.enabled = false;
            audioListener.enabled = false;
        }
    }
}
