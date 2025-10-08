// EndorsementConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "KPIs/EndorsementConfig")]
public class EndorsementConfig : ScriptableObject
{
    [Header("Backend")]
    public string apiBaseUrl = "https://your.api.com";
    public string batchPath = "/api/endorsements/batch";
    public string authHeaderName = "Authorization";
    public string authHeaderValue = "Bearer <TOKEN>"; // o deja vacío si no hay auth

    [Header("Networking")]
    public int maxRetries = 5;
    public float initialBackoffSeconds = 1.5f; // exponencial
    public float requestTimeoutSeconds = 15f;

    [Header("Offline queue")]
    public string queueFileName = "endorsement_queue.json";
}
