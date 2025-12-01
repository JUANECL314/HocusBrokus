using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
[RequireComponent(typeof(SphereCollider))]
public class ButtonActivation : MonoBehaviourPun, ISubject
{
    public int buttonID;
    public float interactionDistance = 2f;
    public bool isEnabled = true;
    public bool isPressed = false;
    private bool canInteract = false;

    public KeyCode teclaAbrir = KeyCode.R;
    public Transform localPlayer;
    public GameObject[] pipes;

    private Renderer rend;
    private List<IObserver> observers = new List<IObserver>();

    public void AddObserver(IObserver obs) => observers.Add(obs);
    public void RemoveObserver(IObserver obs) => observers.Remove(obs);

    void Start()
    {

        rend = GetComponent<Renderer>();

        if (rend != null) rend.material.color = isEnabled ? Color.red : Color.gray;

        foreach (GameObject pipe in pipes)
        {
            Renderer r = pipe.GetComponent<Renderer>();
            if (r != null) r.material.color = Color.gray;
        }

        StartCoroutine(FindLocalPlayer());
        var trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 2f;
        
    }

    IEnumerator FindLocalPlayer()
    {
        while (PhotonNetwork.LocalPlayer.TagObject == null)
            yield return null;

        localPlayer = PhotonNetwork.LocalPlayer.TagObject as Transform;
    }

    void Update()
    {
        if(!canInteract || !isEnabled) return;

        if (Input.GetKeyDown(teclaAbrir) && isEnabled)
        {
            isPressed = true;
            SetPressedRPC(isPressed);
            UIControllerPuzzle2.Instance.ShowButtonUI((canInteract && !isPressed), this);

        }
        else if (Input.GetKeyUp(teclaAbrir))
        {
            isPressed = false;
            SetPressedRPC(isPressed);
            UIControllerPuzzle2.Instance.ShowButtonUI((canInteract && !isPressed), this);
        }
        

    }

    private void OnTriggerEnter(Collider other)
    {
        if (localPlayer == null) return;
        if (other.transform != localPlayer) return;



        canInteract = true;
        UIControllerPuzzle2.Instance.ShowButtonUI(canInteract, this);

    }

    private void OnTriggerExit(Collider other)
    {
        if (localPlayer == null) return;
        if (other.transform != localPlayer) return;

        canInteract = false;
        UIControllerPuzzle2.Instance.ShowButtonUI(canInteract, this);


    }
    private void SetPressedRPC(bool state)
    {
       
        photonView.RPC(nameof(RPC_SetPressed), RpcTarget.AllBuffered,state);
       
    }
    [PunRPC]
    void RPC_NotifyAll(int id, bool state)
    {
        foreach (var obs in observers)
            obs.OnNotify(id, state);
    }

    [PunRPC]
    void RPC_SetEnabledAll(bool state)
    {

        SetEnabled(state);
    }

    public void SetEnabled(bool state)
    {

        isEnabled = state;
        if (rend != null)
            rend.material.color = isEnabled ? Color.red : Color.gray;
    }

    [PunRPC]
    void RPC_SetPressed(bool state)
    {
       isPressed = state;

        if (rend != null)
            rend.material.color = isPressed ? Color.green : (isEnabled ? Color.red : Color.gray);

        foreach (GameObject pipe in pipes)
        {
            Renderer r = pipe.GetComponent<Renderer>();
            if (r == null) continue;

            Material mat = r.material;
            if (isPressed)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.green * 1.4f);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
        }

        if (isEnabled)
            photonView.RPC(nameof(RPC_NotifyAll), RpcTarget.All, buttonID, isPressed);
    }

}