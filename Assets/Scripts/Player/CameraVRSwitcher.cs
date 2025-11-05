using UnityEngine;
using UnityEngine.XR.Management;
using Photon.Pun;

public class CameraVRSwitcher : MonoBehaviourPun
{
    [SerializeField] Camera normalCamera;  // tu cámara clásica del Player
    [SerializeField] GameObject xrOrigin;  // XR Origin (Action-based)

    void Start()
    {
        bool isMine = photonView == null || photonView.IsMine;
        bool xrActive = XRGeneralSettings.Instance?.Manager?.activeLoader != null;

        if (!isMine)
        {
            // Jugador remoto: desactivamos cámaras y VR
            if (normalCamera) normalCamera.enabled = false;
            if (xrOrigin) xrOrigin.SetActive(false);
            return;
        }

        if (xrActive)
        {
            // VR ON: cámara clásica OFF
            if (normalCamera) normalCamera.gameObject.SetActive(false);
            if (xrOrigin) xrOrigin.SetActive(true);
        }
        else
        {
            // VR OFF: XR OFF, usamos cámara clásica
            if (normalCamera) normalCamera.gameObject.SetActive(true);
            if (xrOrigin) xrOrigin.SetActive(false);
        }
    }
}
