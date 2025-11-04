using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class PlayerCamera : MonoBehaviourPun
{
    private Camera playerCamera;
    private AudioListener audioListener;

    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>(true);
        audioListener = GetComponentInChildren<AudioListener>(true);

        // Siempre desactivar al inicio
        if (playerCamera != null) playerCamera.enabled = false;
        if (audioListener != null) audioListener.enabled = false;
    }

    public void EnableLocalCamera()
    {
        if (playerCamera != null) playerCamera.enabled = true;
        if (audioListener != null) audioListener.enabled = true;
    }

    void Start()
    {
        // Activar automáticamente si es jugador local
        if (photonView == null || photonView.IsMine)
            EnableLocalCamera();
    }
}
