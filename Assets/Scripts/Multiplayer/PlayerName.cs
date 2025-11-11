using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerName : MonoBehaviourPunCallbacks
{
    public string playerName;
    public static readonly Dictionary<Player, PlayerName> jugadoresActivos = new();

    void Start()
    {
        playerName = PhotonNetwork.NickName;
        photonView.RPC(nameof(SyncName), RpcTarget.AllBuffered, playerName);

        jugadoresActivos[photonView.Owner] = this;
    }

    void OnDestroy()
    {
        if (jugadoresActivos.ContainsKey(photonView.Owner))
            jugadoresActivos.Remove(photonView.Owner);
    }

    [PunRPC]
    void SyncName(string name)
    {
        playerName = name;
        gameObject.name = name;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (jugadoresActivos.ContainsKey(otherPlayer))
            jugadoresActivos.Remove(otherPlayer);
    }
}
