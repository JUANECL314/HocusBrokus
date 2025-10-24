using Photon.Pun;
using UnityEngine;

public class AuxControl : MonoBehaviour
{

    // Update is called once per frame

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

    }
    void Update()
        {
            // Solo ejecuta si el jugador es el Master Client
            if (PhotonNetwork.IsMasterClient)
            {
                // Si se presiona la tecla R
                if (Input.GetKeyDown(KeyCode.R))
                {
                    PhotonNetwork.LoadLevel("TownRoom");
                }
            }
        }
        
    }
