using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomListManager : MonoBehaviourPunCallbacks
{
    [Header("Referencias UI")]
    public GameObject roomItemPrefab;
    public Transform content; // Donde se instancian las salas
    public Button refreshButton;

    private Dictionary<string, RoomInfo> roomCache = new Dictionary<string, RoomInfo>();

    void Start()
    {
        refreshButton.onClick.AddListener(RefreshList);
    }

    public void RefreshList()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (RoomInfo info in roomCache.Values)
        {
            if (info.RemovedFromList) continue;
            GameObject item = Instantiate(roomItemPrefab, content);
            item.transform.Find("RoomName").GetComponent<TMP_Text>().text = info.Name;
            item.transform.Find("PlayerCount").GetComponent<TMP_Text>().text = $"{info.PlayerCount}/{info.MaxPlayers}";
            Button joinButton = item.transform.Find("JoinButton").GetComponent<Button>();

            joinButton.onClick.AddListener(() =>
            {
                PhotonNetwork.JoinRoom(info.Name);
            });
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                if (roomCache.ContainsKey(room.Name))
                    roomCache.Remove(room.Name);
            }
            else
            {
                roomCache[room.Name] = room;
            }
        }
        RefreshList();
    }

    
}
