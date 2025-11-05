using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMagicInput : MonoBehaviourPun
{
    public Magic magic;
    private PhotonView pv;

    private void Reset()
    {
        if (magic == null) magic = GetComponent<Magic>();
    }

    private void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (magic == null) magic = GetComponent<Magic>();
        if (magic == null) magic = GetComponentInChildren<Magic>();
        if (magic == null) Debug.LogWarning("PlayerMagicInput: Magic no asignado.");
    }

    public void OnCast(InputValue value)
    {
        if (!pv.IsMine) return;
        if (magic == null) return;

        if (value.isPressed)
            magic.launchElement();
    }

    public void OnMagicInfo(InputValue value)
    {
        if (!pv.IsMine) return;
        if (magic == null) return;

        if (value.isPressed)
            magic.elementDescription();
    }
}
