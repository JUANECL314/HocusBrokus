using Photon.Pun;
using UnityEngine;

public class PlayerRole : MonoBehaviourPun
{
    public TeleportRole role = TeleportRole.User;

    // Este jugador pide que otro jugador tenga el rol Controller
    public void RequestRoleChange(int targetPlayerActorNumber)
    {
        photonView.RPC(nameof(RPC_ChangeRole), RpcTarget.AllBuffered, targetPlayerActorNumber);
    }

    [PunRPC]
    void RPC_ChangeRole(int targetActorNumber)
    {
        foreach (var p in PhotonNetwork.PlayerList)
        {
            Transform playerTransform = p.TagObject as Transform;
            if (playerTransform == null) continue;

            PlayerRole pr = playerTransform.GetComponent<PlayerRole>();
            TeleportController tc = playerTransform.GetComponent<TeleportController>();
            if (pr == null || tc == null) continue;

            // Asignar rol
            pr.role = (p.ActorNumber == targetActorNumber) ? TeleportRole.Controller : TeleportRole.User;

            // Actualizar TeleportController según el rol
            tc.canRotatePositions = (pr.role == TeleportRole.Controller);
        }
    }
}
