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
    public GameObject texto;
    public GameObject uiCrearSalas;
    public GameObject jugador;
    public KeyCode teclaAbrirMenu = KeyCode.E;
    private bool menuAbierto = false;
    private void Start()
    {
        botonCrearUnirSalas.SetActive(false);
        texto.SetActive(false);
        uiCrearSalas.SetActive(false);
    }


    void Update(){
        if (Input.GetKeyDown(teclaAbrirMenu))
        {
            if (menuAbierto)
                CerrarMenu();
            else
                AbrirMenu();
        }
    }

    public void ActivarBoton()
    {
        botonCrearUnirSalas.SetActive(true);
        texto.SetActive(true);
    }

    public void DesactivarBoton()
    {
        botonCrearUnirSalas.SetActive(false);
        texto.SetActive(false);
    }

    public void AbrirMenu()
    {
        uiCrearSalas.SetActive(true);
        jugador.GetComponent<FreeFlyDebug>().enabled= false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        menuAbierto = true;
    }

    public void CerrarMenu()
    {
        uiCrearSalas.SetActive(false);
        jugador.GetComponent<FreeFlyDebug>().enabled = true;

        // Ocultar cursor y bloquearlo al centro
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        menuAbierto = false;
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
