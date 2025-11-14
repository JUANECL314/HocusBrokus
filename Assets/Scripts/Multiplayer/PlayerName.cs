using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerName : MonoBehaviourPunCallbacks
{
    public string playerName;

    public static readonly Dictionary<Player, PlayerName> jugadoresActivos = new();
    public static event Action OnJugadoresActualizados;

    void Start()
    {
        playerName = PhotonNetwork.NickName;

        photonView.RPC(nameof(SyncName), RpcTarget.AllBuffered, playerName);

        jugadoresActivos[photonView.Owner] = this;
        OnJugadoresActualizados?.Invoke();
    }

    void OnDestroy()
    {
        if (jugadoresActivos.ContainsKey(photonView.Owner))
        {
            jugadoresActivos.Remove(photonView.Owner);
            OnJugadoresActualizados?.Invoke();
        }
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
        {
            jugadoresActivos.Remove(otherPlayer);
            OnJugadoresActualizados?.Invoke();
        }
    }
}
