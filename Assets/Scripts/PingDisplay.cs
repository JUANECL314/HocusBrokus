using UnityEngine;
using Photon.Pun;  // Make sure Photon PUN is imported

public class PingDisplay : MonoBehaviour
{
    [Tooltip("How often (in seconds) to update the ping display")]
    public float updateInterval = 1f;

    [Tooltip("Optional: Assign a UI Text to display ping on-screen")]
    public TMPro.TextMeshProUGUI pingText;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            int ping = PhotonNetwork.GetPing(); // Ping in milliseconds
            Debug.Log($"[Photon Ping] {ping} ms");

            if (pingText != null)
                pingText.text = $"Ping: {ping} ms";
        }
    }
}
