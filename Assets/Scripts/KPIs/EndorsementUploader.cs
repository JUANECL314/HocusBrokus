using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class EndorsementUploader : MonoBehaviour
{
    public static EndorsementUploader Instance { get; private set; }
    [SerializeField] private EndorsementConfig config;

    [Header("Seguridad (cola local)")]
    [SerializeField] private bool encryptQueueFile = true;
    [SerializeField] private string localEncryptionPassphrase = "Encryption";

    [Header("Compatibilidad (cola y endpoint)")]
    [SerializeField] private string compatQueueFileName = "compat_queue.json";
    [SerializeField] private string compatibilityPath = "/api/compatibility/batch";

    // --------- Estructuras de cola: ENDORSEMENTS ----------
    [Serializable]
    private class BatchWrapper
    {
        public List<EndorsementPayload> items = new();
        public string idempotencyKey;
    }

    [Serializable]
    private class QueueFile
    {
        public List<BatchWrapper> pending = new();
    }

    // --------- Estructuras de cola: COMPATIBILITY ----------
    [Serializable]
    private class CompatBatchWrapper
    {
        public List<CompatibilityVotePayload> items = new();
        public string idempotencyKey;
    }

    [Serializable]
    private class CompatQueueFile
    {
        public List<CompatBatchWrapper> pending = new();
    }

    // --- Configuración del serializer ---
    private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        Converters = new List<JsonConverter> { new StringEnumConverter() },
        NullValueHandling = NullValueHandling.Ignore
    };

    private string QueuePath => Path.Combine(Application.persistentDataPath, config.queueFileName);
    private string CompatQueuePath => Path.Combine(Application.persistentDataPath, compatQueueFileName);

    private bool _uploadingEndorse;
    private bool _uploadingCompat;
    public bool IsUploadingEndorse => _uploadingEndorse;
    public bool IsUploadingCompat => _uploadingCompat;

    public bool HasPendingEndorsements
    {
        get
        {
            var q = LoadQueue();
            return q.pending.Count > 0 || _uploadingEndorse;
        }
    }

    public bool HasPendingCompatibility
    {
        get
        {
            var q = LoadCompatQueue();
            return q.pending.Count > 0 || _uploadingCompat;
        }
    }

    public bool HasAnyPendingUploads => HasPendingEndorsements || HasPendingCompatibility;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (config == null)
            config = Resources.Load<EndorsementConfig>("EndorsementConfig");

        EnsureQueueFile();
        EnsureCompatQueueFile();
    }

    // ===================== ENDORSEMENTS: helpers de cola =====================

    private void EnsureQueueFile()
    {
        if (File.Exists(QueuePath)) return;

        var q = new QueueFile();
        var json = JsonConvert.SerializeObject(q, _jsonSettings);

        if (encryptQueueFile)
        {
            var enc = AesCrypto.EncryptString(json, localEncryptionPassphrase);
            File.WriteAllText(QueuePath, enc);
        }
        else
        {
            File.WriteAllText(QueuePath, json);
        }
    }

    private QueueFile LoadQueue()
    {
        try
        {
            if (!File.Exists(QueuePath)) return new QueueFile();

            string content = File.ReadAllText(QueuePath);

            if (encryptQueueFile)
            {
                try
                {
                    string json = AesCrypto.DecryptString(content, localEncryptionPassphrase);
                    return JsonConvert.DeserializeObject<QueueFile>(json) ?? new QueueFile();
                }
                catch
                {
                    // Fallback: puede que sea JSON plano antiguo
                    try
                    {
                        return JsonConvert.DeserializeObject<QueueFile>(content) ?? new QueueFile();
                    }
                    catch
                    {
                        return new QueueFile();
                    }
                }
            }
            else
            {
                return JsonConvert.DeserializeObject<QueueFile>(content) ?? new QueueFile();
            }
        }
        catch
        {
            return new QueueFile();
        }
    }

    private void SaveQueue(QueueFile q)
    {
        var json = JsonConvert.SerializeObject(q, _jsonSettings);

        if (encryptQueueFile)
        {
            var enc = AesCrypto.EncryptString(json, localEncryptionPassphrase);
            File.WriteAllText(QueuePath, enc);
            Debug.Log("[Uploader] Datos encriptados (endorsements): " + enc.Substring(0, Mathf.Min(50, enc.Length)) + "...");
        }
        else
        {
            File.WriteAllText(QueuePath, json);
        }
    }
    public void SetAuthToken(string jwt)
    {
        if (config == null) return;
        config.authHeaderName = "Authorization";
        config.authHeaderValue = "Bearer " + jwt;
    }

    // ===================== COMPATIBILITY: helpers de cola =====================

    private void EnsureCompatQueueFile()
    {
        if (File.Exists(CompatQueuePath)) return;

        var q = new CompatQueueFile();
        var json = JsonConvert.SerializeObject(q, _jsonSettings);

        if (encryptQueueFile)
        {
            var enc = AesCrypto.EncryptString(json, localEncryptionPassphrase);
            File.WriteAllText(CompatQueuePath, enc);
        }
        else
        {
            File.WriteAllText(CompatQueuePath, json);
        }
    }

    private CompatQueueFile LoadCompatQueue()
    {
        try
        {
            if (!File.Exists(CompatQueuePath)) return new CompatQueueFile();

            string content = File.ReadAllText(CompatQueuePath);

            if (encryptQueueFile)
            {
                try
                {
                    string json = AesCrypto.DecryptString(content, localEncryptionPassphrase);
                    return JsonConvert.DeserializeObject<CompatQueueFile>(json) ?? new CompatQueueFile();
                }
                catch
                {
                    // Fallback a JSON plano
                    try
                    {
                        return JsonConvert.DeserializeObject<CompatQueueFile>(content) ?? new CompatQueueFile();
                    }
                    catch
                    {
                        return new CompatQueueFile();
                    }
                }
            }
            else
            {
                return JsonConvert.DeserializeObject<CompatQueueFile>(content) ?? new CompatQueueFile();
            }
        }
        catch
        {
            return new CompatQueueFile();
        }
    }

    private void SaveCompatQueue(CompatQueueFile q)
    {
        var json = JsonConvert.SerializeObject(q, _jsonSettings);

        if (encryptQueueFile)
        {
            var enc = AesCrypto.EncryptString(json, localEncryptionPassphrase);
            File.WriteAllText(CompatQueuePath, enc);
            Debug.Log("[Uploader] Datos encriptados (compat): " + enc.Substring(0, Mathf.Min(50, enc.Length)) + "...");
        }
        else
        {
            File.WriteAllText(CompatQueuePath, json);
        }
    }

    // ===================== API pública: ENQUEUE =====================

    public void EnqueueAndSend(List<EndorsementPayload> batch)
    {
        var q = LoadQueue();
        q.pending.Add(new BatchWrapper
        {
            items = batch,
            idempotencyKey = Guid.NewGuid().ToString("N")
        });
        SaveQueue(q);

        if (!_uploadingEndorse)
            StartCoroutine(ProcessQueue());
    }

    public void EnqueueAndSendCompatibility(List<CompatibilityVotePayload> votes)
    {
        var q = LoadCompatQueue();
        q.pending.Add(new CompatBatchWrapper
        {
            items = votes,
            idempotencyKey = Guid.NewGuid().ToString("N")
        });
        SaveCompatQueue(q);

        if (!_uploadingCompat)
            StartCoroutine(ProcessCompatQueue());
    }

    // ===================== Loops de envío =====================

    private System.Collections.IEnumerator ProcessQueue()
    {
        _uploadingEndorse = true;
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

                if (req.responseCode == 409 || req.responseCode == 429 ||
                    (req.responseCode >= 500 && req.responseCode < 600) || req.isNetworkError)
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
                break; // detener para evitar loop infinito
            }
        }

        _uploadingEndorse = false;
    }

    private System.Collections.IEnumerator ProcessCompatQueue()
    {
        _uploadingCompat = true;
        var q = LoadCompatQueue();

        while (q.pending.Count > 0)
        {
            var current = q.pending[0];
            bool ok = false;
            float backoff = config.initialBackoffSeconds;

            for (int attempt = 1; attempt <= config.maxRetries; attempt++)
            {
                var req = BuildRequestCompatibility(current);
                req.timeout = Mathf.RoundToInt(config.requestTimeoutSeconds);
                yield return req.SendWebRequest();

                if (!req.isNetworkError && !req.isHttpError && req.responseCode >= 200 && req.responseCode < 300)
                {
                    ok = true;
                    break;
                }

                if (req.responseCode == 409 || req.responseCode == 429 ||
                    (req.responseCode >= 500 && req.responseCode < 600) || req.isNetworkError)
                {
                    yield return new WaitForSeconds(backoff);
                    backoff *= 2f;
                }
                else
                {
                    Debug.LogWarning($"[CompatUploader] Error {req.responseCode}: {req.downloadHandler.text}");
                    break;
                }
            }

            if (ok)
            {
                q.pending.RemoveAt(0);
                SaveCompatQueue(q);
            }
            else
            {
                break; // detener para evitar loop infinito
            }
        }

        _uploadingCompat = false;
    }

    // ===================== Builders de request =====================

    private UnityWebRequest BuildRequest(BatchWrapper batch)
    {
        string url = config.apiBaseUrl.TrimEnd('/') + config.batchPath;

        Debug.Log("[Uploader] Enviando Endorsements a: " + url);

        var json = JsonConvert.SerializeObject(batch, _jsonSettings);

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


    private UnityWebRequest BuildRequestCompatibility(CompatBatchWrapper batch)
    {
        string url = config.apiBaseUrl.TrimEnd('/') + compatibilityPath;

        Debug.Log("[CompatUploader] Enviando Compatibility a: " + url);

        var json = JsonConvert.SerializeObject(batch, _jsonSettings);

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
