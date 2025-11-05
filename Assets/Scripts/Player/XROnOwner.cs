using UnityEngine;
using Photon.Pun;

public class XROnOwner : MonoBehaviourPun
{
    [SerializeField] GameObject xrOrigin;     // XR Origin (Action-based)
    [SerializeField] Camera nonVrCamera;    // tu cámara clásica

    void Start()
    {
        bool mine = photonView == null || photonView.IsMine;
        if (xrOrigin) xrOrigin.SetActive(mine);     // solo el owner usa XR
        if (nonVrCamera) nonVrCamera.gameObject.SetActive(!mine);
    }
}
