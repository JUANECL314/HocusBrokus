using TMPro;
using UnityEngine;

public class PlayerListDistance : MonoBehaviour
{
    public TMP_Text playerListText;
    public float updateRate = 0.5f; // Actualiza cada 0.5 segundos

    private PlayerNameListInfo localPlayer;

    void Start()
    {
        InvokeRepeating(nameof(UpdatePlayerList), 1f, updateRate);
    }

    void UpdatePlayerList()
    {
        if (localPlayer == null)
            localPlayer = FindLocalPlayer();

        if (localPlayer == null)
        {
            playerListText.text = "Esperando jugador local...";
            return;
        }

        PlayerNameListInfo[] allPlayers = FindObjectsOfType<PlayerNameListInfo>();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Jugadores conectados:");

        foreach (var p in allPlayers)
        {
            float dist = Vector3.Distance(localPlayer.transform.position, p.transform.position);
            sb.AppendLine($"- {p.playerName} ({dist:F1} m)");
        }

        playerListText.text = sb.ToString();
    }

    PlayerNameListInfo FindLocalPlayer()
    {
        foreach (var p in FindObjectsOfType<PlayerNameListInfo>())
        {
            if (p.photonView.IsMine)
                return p;
        }
        return null;
    }
}
