using UnityEngine;
using Photon.Pun;

public class IrisLookController : MonoBehaviourPun
{
    public FreeFlyCameraMulti playerController;
    public Transform irisQuad;
    public float lookRadius = 0.1f;
    public float smoothSpeed = 5f;

    private Vector3 baseLocalPos;

    void Start()
    {
        baseLocalPos = irisQuad.localPosition;

        // Automatically assign for the local player only
        if (photonView.IsMine)
        {
            playerController = GetComponentInParent<FreeFlyCameraMulti>();
        }
    }

    void LateUpdate()
    {
        if (!photonView.IsMine) return;
        if (playerController == null || irisQuad == null) return;

        // Get direction camera is facing
        Vector3 camForward = playerController.cameraTransform.forward;

        // Offset the iris based on that direction (simulating eye look)
        Vector3 localOffset = new Vector3(camForward.x, camForward.y, 0) * lookRadius;
        Vector3 targetLocalPos = baseLocalPos + localOffset;

        irisQuad.localPosition = Vector3.Lerp(
            irisQuad.localPosition,
            targetLocalPos,
            Time.deltaTime * smoothSpeed
        );
    }
}
