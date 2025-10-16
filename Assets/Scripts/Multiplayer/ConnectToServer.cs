using UnityEngine;
using Photon.Pun;
using Photon.Voice.PUN;
using UnityEngine.SceneManagement;
public class ConnectToServer : MonoBehaviourPunCallbacks
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    
    public void conectarServidor()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }
    public override void OnJoinedLobby()
    {
        // Conectar a Photon Voice antes de entrar a la sala


        PhotonNetwork.LoadLevel("Lobby");
    }
}
