using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Auxiliar : MonoBehaviourPunCallbacks
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public void CaveLevel()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Cave");
        }
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        // Verifica si quien se fue era el Master Client
        if (otherPlayer.IsMasterClient)
        {
            

          
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
