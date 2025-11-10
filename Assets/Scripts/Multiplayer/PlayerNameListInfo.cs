using Photon.Pun;
using UnityEngine;

public class PlayerNameListInfo : MonoBehaviourPun
{
    public string playerName;

    void Start()
    {
        if (photonView.IsMine)
        {
            playerName = PhotonNetwork.NickName;
            photonView.RPC("SyncName", RpcTarget.AllBuffered, playerName);
        }
    }

    [PunRPC]
    void SyncName(string name)
    {
        playerName = name;
        gameObject.name = $"{name}";
    }
}
