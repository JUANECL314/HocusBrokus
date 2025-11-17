using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class ButtonActivation : MonoBehaviourPun, ISubject
{
    public int buttonID;

    public float interactionDistance = 2f;

    public bool isEnabled = true;

    private bool isPressed = false;

    public Transform localPlayer;

    private List<IObserver> observers = new List<IObserver>();

    public void AddObserver(IObserver obs ) => observers.Add( obs );

    public void RemoveObserver(IObserver obs) => observers.Remove(obs);
    private Renderer rend;
    void Start()
    {
        StartCoroutine(FindLocalPlayer());
        rend = GetComponent<Renderer>();

        
        if (rend != null && isEnabled) rend.material.color = Color.red;
    }

    IEnumerator FindLocalPlayer()
    {
        while (PhotonNetwork.LocalPlayer.TagObject == null)
        {
            yield return null; 
        }

        localPlayer = PhotonNetwork.LocalPlayer.TagObject as Transform;
        Debug.Log($"[Botón {buttonID}] Local player asignado: {localPlayer.name}");
    }

    void Update()
    {
        if (!isEnabled || localPlayer == null) return;

        float dist = Vector3.Distance(localPlayer.position, transform.position);
        
        
        if (dist <= interactionDistance && Input.GetKeyDown(KeyCode.R))
        {
            isPressed = true;
        } else if (Input.GetKeyUp(KeyCode.R) && (dist <= interactionDistance || dist > interactionDistance))
        {
            isPressed = false;
            
        }
        photonView.RPC("RPC_NotifyAll", RpcTarget.All, buttonID, isPressed);
        if(isEnabled) photonView.RPC("RPC_SetEnabledAll", RpcTarget.All, true);
        photonView.RPC("RPC_SetPressed", RpcTarget.All, isPressed);

    }

    [PunRPC]
    void RPC_NotifyAll(int id, bool state)
    {
        Debug.Log($"[Master] Botón {id} fue presionado. Estado = {state}");
        foreach (var obs in observers)
            obs.OnNotify(id, state);
    }
    [PunRPC]
    void RPC_SetEnabledAll(bool state)
    {
        SetEnabled(state);
    }
    public void SetEnabled(bool e)
    {
        isEnabled = e;
        rend.material.color = isEnabled ? Color.red : Color.gray;
      
    }
    [PunRPC]
    void RPC_SetPressed(bool state)
    {
        SetPressed(state);
    }
    public void SetPressed(bool e)
    {
        rend.material.color = e ? Color.green : Color.red;
    }
}

