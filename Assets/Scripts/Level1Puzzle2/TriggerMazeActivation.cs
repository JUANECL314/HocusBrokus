using Photon.Pun;
using UnityEngine;

public class TriggerMazeActivation : MonoBehaviourPun
{
    private MazeStateMachine maze;

    private void Start()
    {
        // Buscar el singleton una vez que exista en la escena
        maze = MazeStateMachine.Instance;
        if (maze == null)
            Debug.LogError("MazeStateMachine.Instance no encontrado en la escena.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (maze == null) return;
        if (other.GetComponent<PhotonView>().IsMine)
        {
            MazeStateMachine.Instance.panelMagicLeft.SetActive(true);
        }
        if (!PhotonNetwork.IsMasterClient) return;

        maze.countWizards++;
        maze.OnPlayersChanged();

        
        maze.photonView.RPC(nameof(MazeStateMachine.UpdateWizardsUIRPC), RpcTarget.All, maze.countWizards);


    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (maze == null) return;
        if (other.GetComponent<PhotonView>().IsMine)
        {
            MazeStateMachine.Instance.panelMagicLeft.SetActive(false);
        }
        if (!PhotonNetwork.IsMasterClient) return;

        maze.countWizards--;
        maze.OnPlayersChanged();

        
        maze.photonView.RPC(nameof(MazeStateMachine.UpdateWizardsUIRPC), RpcTarget.All, maze.countWizards);

    }


}
