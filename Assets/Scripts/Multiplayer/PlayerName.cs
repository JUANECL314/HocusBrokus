using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerName : MonoBehaviourPunCallbacks
{
    [Header("Debug")]
    [SerializeField] private bool logDebug = true;

    [Header("Datos")]
    public string playerName;

    // Mapa global: Player de Photon -> componente PlayerName
    public static readonly Dictionary<Player, PlayerName> jugadoresActivos = new();

    // Referencia rápida al PlayerName local (el que controla este cliente)
    public static PlayerName LocalPlayerName { get; private set; }

    public static event Action OnJugadoresActualizados;

    void Start()
    {
        // Registrar SIEMPRE este PlayerName en el diccionario
        var owner = photonView.Owner;
        jugadoresActivos[owner] = this;

        if (logDebug)
        {
            Debug.Log($"[PlayerName] Start en cliente {PhotonNetwork.LocalPlayer.ActorNumber} " +
                      $"=> owner={owner.ActorNumber}, IsMine={photonView.IsMine}", this);
        }

        // Si este objeto pertenece al jugador LOCAL, este es "mi" PlayerName
        if (owner == PhotonNetwork.LocalPlayer)
        {
            LocalPlayerName = this;

            // Solo el dueño decide el nombre y lo manda por RPC
            playerName = PhotonNetwork.NickName;
            if (logDebug)
            {
                Debug.Log($"[PlayerName] Soy el local ({owner.ActorNumber}), " +
                          $"asigno nombre '{playerName}' y hago RPC.", this);
            }

            photonView.RPC(nameof(SyncName), RpcTarget.AllBuffered, playerName);
        }

        OnJugadoresActualizados?.Invoke();
    }

    void OnDestroy()
    {
        var owner = photonView.Owner;

        if (jugadoresActivos.ContainsKey(owner))
            jugadoresActivos.Remove(owner);

        if (LocalPlayerName == this)
            LocalPlayerName = null;

        if (logDebug)
        {
            Debug.Log($"[PlayerName] OnDestroy en cliente {PhotonNetwork.LocalPlayer.ActorNumber}, " +
                      $"owner={owner.ActorNumber}", this);
        }

        OnJugadoresActualizados?.Invoke();
    }

    [PunRPC]
    void SyncName(string name)
    {
        playerName = name;
        gameObject.name = name;

        if (logDebug)
        {
            Debug.Log($"[PlayerName] RPC SyncName en cliente {PhotonNetwork.LocalPlayer.ActorNumber}, " +
                      $"owner={photonView.Owner.ActorNumber}, name={name}", this);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (jugadoresActivos.ContainsKey(otherPlayer))
        {
            jugadoresActivos.Remove(otherPlayer);

            if (logDebug)
            {
                Debug.Log($"[PlayerName] OnPlayerLeftRoom en cliente {PhotonNetwork.LocalPlayer.ActorNumber}, " +
                          $"sale={otherPlayer.ActorNumber}", this);
            }

            OnJugadoresActualizados?.Invoke();
        }
    }
}
