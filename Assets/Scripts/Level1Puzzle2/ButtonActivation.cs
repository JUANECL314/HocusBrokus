using Photon.Pun;
using System;
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
    public KeyCode teclaAbrir = KeyCode.R;
    public Transform localPlayer;
    public GameObject panelUI;

    private List<IObserver> observers = new List<IObserver>();

    public void AddObserver(IObserver obs ) => observers.Add( obs );

    public void RemoveObserver(IObserver obs) => observers.Remove(obs);
    private Renderer rend;
    public GameObject[] pipes;
    void Start()
    {
        panelUI.SetActive(false);
        StartCoroutine(FindLocalPlayer());
        rend = GetComponent<Renderer>();


        if (rend != null && isEnabled) rend.material.color = Color.red;
        foreach (GameObject pipe in pipes)
        {

            pipe.GetComponent<Renderer>().material.color = Color.gray;
        }
        StartCoroutine(CheckStatus());
    }
    IEnumerator FindLocalPlayer()
    {
        while (PhotonNetwork.LocalPlayer.TagObject == null)
        {
            yield return null; 
        }

        localPlayer = PhotonNetwork.LocalPlayer.TagObject as Transform;
    }

    void Update()
    {
        
        
        


    }

    IEnumerator CheckStatus()
    {
        yield return new WaitForSeconds(5f);
        
        float dist = Vector3.Distance(localPlayer.position, transform.position);

        if (dist <= interactionDistance)
        {
            if (!panelUI.activeSelf)
            { panelUI.SetActive(true); }
            else if (panelUI.activeSelf)
            { panelUI.SetActive(true); }
        }
        else
        {
            if (panelUI.activeSelf)
            { panelUI.SetActive(false); }
            else
            {
                panelUI.SetActive(false);
            }
        }

        if (dist <= interactionDistance && Input.GetKeyDown(teclaAbrir))
        {
            isPressed = true;
        }
        else if (Input.GetKeyUp(teclaAbrir) && (dist <= interactionDistance || dist > interactionDistance))
        {
            isPressed = false;


        }
        photonView.RPC("RPC_NotifyAll", RpcTarget.All, buttonID, isPressed);
        if (isEnabled) photonView.RPC("RPC_SetEnabledAll", RpcTarget.All, true);
        photonView.RPC("RPC_SetPressed", RpcTarget.All, isPressed);
        StartCoroutine(CheckStatus());
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
        foreach (GameObject pipe in pipes)
        {
            Renderer renderer = pipe.GetComponent<Renderer>();
            Material pipeMaterial = renderer.material;
            if (e)
            {
                pipeMaterial.EnableKeyword("_EMISSION");
                pipeMaterial.SetColor("_EmissionColor", Color.green * 1.4f);
            }
            else
            {
                pipeMaterial.DisableKeyword("_EMISSION");
                pipeMaterial.SetColor("_EmissionColor", Color.black);
            } 
        }
    }    
}

