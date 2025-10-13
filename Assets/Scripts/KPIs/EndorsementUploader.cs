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

    // --- Configuración del serializer ---
    private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        Converters = new List<JsonConverter> { new StringEnumConverter() },
        NullValueHandling = NullValueHandling.Ignore
    };

    private string QueuePath => Path.Combine(Application.persistentDataPath, config.queueFileName);
    private bool _uploading;

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
    }

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

            // Si está cifrado, descifra. Si no, intenta parsear plano (migración).
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
            Debug.Log("[Uploader] Datos encriptados: " + enc.Substring(0, 50) + "...");
        }
        else
        {
            File.WriteAllText(QueuePath, json);
        }
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

        if (!_uploading)
            StartCoroutine(ProcessQueue());
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

                // reintento con backoff exponencial
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

        _uploading = false;
    }

    private UnityWebRequest BuildRequest(BatchWrapper batch)
    {
        string url = config.apiBaseUrl.TrimEnd('/') + config.batchPath;

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
