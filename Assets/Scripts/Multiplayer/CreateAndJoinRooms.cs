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
    public GameObject ui;
    public TextMeshProUGUI ingresar;
    bool condicion = false;
    private void Start()
    {

        // IniciarSesion.instance.GuardarUsuario();
        ui.SetActive(false);
        ingresar.text = "Ingresar sala";
     }
    

    public void abrirSala()
    {
        if (!condicion)
        {
            ingresar.text = "Salir";
            ui.SetActive(true);
            condicion = true;
        }
        else
        {
            ingresar.text = "Ingresar sala";
            ui.SetActive(false);
            condicion = false;
        }
        
        
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
