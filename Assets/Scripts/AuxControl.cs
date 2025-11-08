using Photon.Pun;
using UnityEngine;

public class AuxControl : MonoBehaviourPun
{
    public GameObject canvas;


    private void Start()
    {
        canvas.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (other.CompareTag("Portal"))
        {
            canvas.SetActive(true);
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        
        if (!PhotonNetwork.IsMasterClient) return;

        if (other.CompareTag("Portal"))
        {
            canvas.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("Canvas desactivado por el Master Client.");
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Master recargando TownRoom...");
            photonView.RPC("RPC_LoadLevel", RpcTarget.AllBuffered, "TownRoom");
        }

        
    }

    public void Level1_1Enter()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("No estás conectado a Photon todavía. Espera antes de cargar el nivel.");
            return;
        }

        if (photonView == null)
        {
            Debug.LogError("photonView es null. Asegúrate de tener un PhotonView en este objeto.");
            return;
        }


        photonView.RPC("RPC_LoadLevel", RpcTarget.AllBuffered, "CavePuzzle1");
    }

    public void Level1_3Enter()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("No estás conectado a Photon todavía. Espera antes de cargar el nivel.");
            return;
        }

        if (photonView == null)
        {
            Debug.LogError(" photonView es null. Asegúrate de tener un PhotonView en este objeto.");
            return;
        }

        photonView.RPC("RPC_LoadLevel", RpcTarget.AllBuffered, "CavePuzzle3");
    }

    
    [PunRPC]
    private void RPC_LoadLevel(string sceneName)
    {
        Debug.Log($"Cambiando escena a: {sceneName}");
        PhotonNetwork.LoadLevel(sceneName);
    }
}
