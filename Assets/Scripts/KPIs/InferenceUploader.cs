using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class InferencePlayerItemDto
{
    public string userId;
    public float pHigh;
    public float pMedium;
    public float pLow;
}

[Serializable]
public class InferencePayloadDto
{
    public string matchId;
    public float teamCoordination;
    public float teamLeadership;
    public float teamRecovery;
    public InferencePlayerItemDto[] players;
}

public class InferenceUploader : MonoBehaviour
{
    public static InferenceUploader Instance { get; private set; }

    [Header("Backend")]
    public string inferenceUrl = "https://hokusbackend-production.up.railway.app/api/inference";
    public bool IsUploadingInference { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Llama esto cuando ya estés en la pantalla de endorsements y
    /// quieras mandar la inferencia a tu backend.
    /// </summary>
    public void SendInference(string matchId)
    {
        var sm = KPIInferenceStateMachine.Instance;
        if (sm == null || !sm.HasResultReady)
        {
            Debug.LogWarning("[InferenceUploader] No hay resultado de inferencia listo.");
            return;
        }

        var res = sm.CurrentResult;

        // Construir players
        List<InferencePlayerItemDto> players = new();
        foreach (var p in res.players)
        {
            players.Add(new InferencePlayerItemDto
            {
                // Ahora mismo usamos actorId tal cual.
                // Más adelante puedes mapear actorId -> userId de backend.
                userId = p.actorId,
                pHigh = p.pHigh,
                pMedium = p.pMedium,
                pLow = p.pLow
            });
        }

        var dto = new InferencePayloadDto
        {
            matchId = matchId,
            teamCoordination = res.teamCoordination,
            teamLeadership = res.teamLeadership,
            teamRecovery = res.teamRecovery,
            players = players.ToArray()
        };
        IsUploadingInference = true;
        StartCoroutine(PostInference(dto));
        sm.MarkResultAsSent(); // opcional, marca que ya lo usamos/enviamos
    }

    private IEnumerator PostInference(InferencePayloadDto dto)
    {
        string json = JsonUtility.ToJson(dto);
        Debug.Log("[InferenceUploader] Enviando JSON a backend: " + json);

        using var req = new UnityWebRequest(inferenceUrl, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[InferenceUploader] Error al enviar inferencia: {req.error} | {req.downloadHandler.text}");
        }
        else
        {
            Debug.Log("[InferenceUploader] Respuesta OK: " + req.downloadHandler.text);
        }

        IsUploadingInference = false;           
    }
}
