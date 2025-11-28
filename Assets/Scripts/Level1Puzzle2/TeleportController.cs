using Photon.Pun;
using UnityEngine;

public class TeleportController : MonoBehaviourPun
{
    public bool canRotatePositions = false; // Solo el Controller puede rotar
    public KeyCode keyRotatePositions = KeyCode.T;

    void Update()
    {
        if (!photonView.IsMine) return;

        if (canRotatePositions && Input.GetKeyDown(keyRotatePositions))
        {
            photonView.RPC(nameof(RPC_RotatePositions), RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_RotatePositions()
    {
        var players = PhotonNetwork.PlayerList;
        if (players.Length < 2) return;

        Vector3[] positions = new Vector3[players.Length];

        // Guardar posiciones
        for (int i = 0; i < players.Length; i++)
        {
            Transform t = players[i].TagObject as Transform;
            if (t == null) return; // Evitar errores
            positions[i] = t.position;
        }

        // Rotar posiciones en cadena
        for (int i = 0; i < players.Length; i++)
        {
            Transform t = players[i].TagObject as Transform;
            t.position = positions[(i + 1) % players.Length];
        }
    }
}
