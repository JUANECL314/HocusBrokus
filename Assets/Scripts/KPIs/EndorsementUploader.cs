using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class EndorsementUploader : MonoBehaviour
{
    public static EndorsementUploader Instance { get; private set; }
    [SerializeField] private EndorsementConfig config;

    [Serializable] private class BatchWrapper { public List<EndorsementPayload> items = new(); public string idempotencyKey; }
    [Serializable] private class QueueFile { public List<BatchWrapper> pending = new(); }

    private string QueuePath => Path.Combine(Application.persistentDataPath, config.queueFileName);
    private bool _uploading;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        if (config == null) config = Resources.Load<EndorsementConfig>("EndorsementConfig");
        EnsureQueueFile();
    }

    private void EnsureQueueFile()
    {
        if (!File.Exists(QueuePath))
        {
            var q = new QueueFile();
            File.WriteAllText(QueuePath, JsonUtility.ToJson(q));
        }
    }

    private QueueFile LoadQueue()
    {
        try { return JsonUtility.FromJson<QueueFile>(File.ReadAllText(QueuePath)) ?? new QueueFile(); }
        catch { return new QueueFile(); }
    }

    private void SaveQueue(QueueFile q)
    {
        File.WriteAllText(QueuePath, JsonUtility.ToJson(q));
    }

    public void EnqueueAndSend(List<EndorsementPayload> batch)
    {
        var q = LoadQueue();
        q.pending.Add(new BatchWrapper
        {
            items = batch,
            idempotencyKey = Guid.NewGuid().ToString("N")
        });
        SaveQueue(q);
        if (!_uploading) StartCoroutine(ProcessQueue());
    }

    private System.Collections.IEnumerator ProcessQueue()
    {
        _uploading = true;
        var q = LoadQueue();

        while (q.pending.Count > 0)
        {
            var current = q.pending[0];
            bool ok = false;
            float backoff = config.initialBackoffSeconds;

            for (int attempt = 1; attempt <= config.maxRetries; attempt++)
            {
                var req = BuildRequest(current);
                req.timeout = Mathf.RoundToInt(config.requestTimeoutSeconds);
                yield return req.SendWebRequest();

                if (!req.isNetworkError && !req.isHttpError && req.responseCode >= 200 && req.responseCode < 300)
                {
                    ok = true;
                    break;
                }

                // reintento
                if (req.responseCode == 409 || req.responseCode == 429 || (req.responseCode >= 500 && req.responseCode < 600) || req.isNetworkError)
                {
                    yield return new WaitForSeconds(backoff);
                    backoff *= 2f;
                }
                else
                {
                    Debug.LogWarning($"[EndorsementUploader] Error {req.responseCode}: {req.downloadHandler.text}");
                    break;
                }
            }

            if (ok)
            {
                q.pending.RemoveAt(0);
                SaveQueue(q);
            }
            else
            {
                // Paramos para evitar loop infinito; reintenta al volver a llamar
                break;
            }
        }

        _uploading = false;
    }

    private UnityWebRequest BuildRequest(BatchWrapper batch)
    {
        string url = config.apiBaseUrl.TrimEnd('/') + config.batchPath;
        var json = JsonUtility.ToJson(batch); 

        var req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(config.authHeaderName) && !string.IsNullOrEmpty(config.authHeaderValue))
        {
            req.SetRequestHeader(config.authHeaderName, config.authHeaderValue);
        }
        req.SetRequestHeader("Idempotency-Key", batch.idempotencyKey);
        return req;
    }
}
