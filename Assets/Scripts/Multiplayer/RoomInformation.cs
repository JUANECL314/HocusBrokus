using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class RoomInformation : MonoBehaviour
{
    public TextMeshProUGUI NameRoom;
    public TextMeshProUGUI currentTotalPlayers;

    public Button joinButton;
    public void UpdateData(string _RoomName, int _CurrentPlayers)
    {
        NameRoom.text = _RoomName;
        currentTotalPlayers.text = _CurrentPlayers.ToString() + "/" + NetworkManager.instance.maxPlayer.ToString();
        AddListenerToButton(_RoomName);
    }

    public void AddListenerToButton(string _RoomName)
    {
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() =>
        {
            JoinRoomOnclick(_RoomName);
        });
    }

    public void JoinRoomOnclick(string _roomName)
    {
        PhotonNetwork.JoinRoom(_roomName);
    }

   
}
