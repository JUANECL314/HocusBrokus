using UnityEngine;
using Photon.Pun;

public class AssignEyeTarget : MonoBehaviourPun
{
    void Start()
    {
        // Only run this on the local player's instance
        if (!photonView.IsMine) return;

        // Get the main camera (your local player's camera)
        Transform camTransform = Camera.main?.transform;
        if (camTransform == null) return;

        // Find all EyeFollowTarget components in children
        EyeFollowTarget[] eyes = GetComponentsInChildren<EyeFollowTarget>();

        foreach (var eye in eyes)
        {
            eye.target = camTransform;
        }
    }
}
