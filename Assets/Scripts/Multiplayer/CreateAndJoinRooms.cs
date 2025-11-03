using Photon.Pun;
using Photon.Voice.Unity;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    public TMP_InputField createInput;
    public TMP_InputField joinInput;

    public GameObject botonCrearUnirSalas;

    public GameObject uiCrearSalas;

    private void Start()
    {
        botonCrearUnirSalas.SetActive(false);
        uiCrearSalas.SetActive(false);
    }

    public void ActivarBoton()
    {
        botonCrearUnirSalas.SetActive(true);
    }

    public void DesactivarBoton()
    {
        botonCrearUnirSalas.SetActive(false);
    }

    public void AbrirMenu()
    {
        uiCrearSalas.SetActive(true);
    }

    public void CerrarMenu()
    {
        uiCrearSalas.SetActive(false);
    }

    public void CreateRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.CreateRoom(createInput.text);
        }
        
    }
    public void JoinRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinRoom(joinInput.text);
        }
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.LoadLevel("TownRoom");
            
            
        }
    }
    

}
