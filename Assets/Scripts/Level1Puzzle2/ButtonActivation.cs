using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonActivation : MonoBehaviourPun, ISubject
{
    public int buttonID;
    public float interactionDistance = 2f;
    public bool isEnabled = true;
    public bool isPressed = false;

    public KeyCode teclaAbrir = KeyCode.R;
    public Transform localPlayer;
    public GameObject panelUI;
    public GameObject[] pipes;

    private Renderer rend;
    private List<IObserver> observers = new List<IObserver>();

    public void AddObserver(IObserver obs) => observers.Add(obs);
    public void RemoveObserver(IObserver obs) => observers.Remove(obs);

    void Start()
    {
        panelUI.SetActive(false);
        rend = GetComponent<Renderer>();

        if (rend != null) rend.material.color = isEnabled ? Color.red : Color.gray;

        foreach (GameObject pipe in pipes)
        {
            Renderer r = pipe.GetComponent<Renderer>();
            if (r != null) r.material.color = Color.gray;
        }

        StartCoroutine(FindLocalPlayer());
    }

    IEnumerator FindLocalPlayer()
    {
        while (PhotonNetwork.LocalPlayer.TagObject == null)
            yield return null;

        localPlayer = PhotonNetwork.LocalPlayer.TagObject as Transform;
    }

    void Update()
    {
        if (localPlayer == null) return;


        float dist = Vector3.Distance(localPlayer.position, transform.position);
        //Debug.Log(dist);
        bool canInteract = dist <= interactionDistance;
        //Debug.Log(canInteract);
        // Solo mostrar el panel al jugador local
        if (dist <= interactionDistance) {
            panelUI.SetActive(true);
        }
        else
        {
            panelUI.SetActive(false);
        }


        if (canInteract && Input.GetKeyDown(teclaAbrir) && photonView.IsMine)
        {
            isPressed = true;


            photonView.RPC(nameof(RPC_SetPressed), RpcTarget.All, isPressed);
            photonView.RPC(nameof(RPC_NotifyAll), RpcTarget.All, buttonID, isPressed);
        }
        else if ((canInteract || !canInteract) && Input.GetKeyUp(teclaAbrir) && photonView.IsMine)
        {
            isPressed = false;


            photonView.RPC(nameof(RPC_SetPressed), RpcTarget.All, isPressed);
            photonView.RPC(nameof(RPC_NotifyAll), RpcTarget.All, buttonID, isPressed);
        }

        
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
        if (rend != null)
            rend.material.color = isEnabled ? Color.red : Color.gray;
    }

    [PunRPC]
    void RPC_SetPressed(bool state)
    {
        SetPressed(state);
    }

    public void SetPressed(bool e)
    {
        isPressed = e;

        if (rend != null)
            rend.material.color = e ? Color.green : (isEnabled ? Color.red : Color.gray);

        foreach (GameObject pipe in pipes)
        {
            Renderer r = pipe.GetComponent<Renderer>();
            if (r == null) continue;

            Material mat = r.material;
            if (e)
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

        // Notificar a observadores solo si está habilitado
        if (isEnabled)
            photonView.RPC(nameof(RPC_NotifyAll), RpcTarget.All, buttonID, isPressed);
    }
}
