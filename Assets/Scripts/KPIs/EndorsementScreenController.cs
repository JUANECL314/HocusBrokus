using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
#endif

public class EndorsementScreenController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform content;     // Vertical Layout
    [SerializeField] private GameObject playerCardPrefab;
    [SerializeField] private Button btnEnviar;
    [SerializeField] private Button btnOmitir;
    [SerializeField] private TextMeshProUGUI txtCounter;

    [Header("Config")]
    [SerializeField] private int maxEndorsements = 3;
    [SerializeField] private string matchIdPrefix = "match";

    [Header("Flujo / Escenas")]
    [SerializeField] private string lobbySceneName = "Lobby";  // ← pon aquí tu escena de lobby

    [Header("Mock (para pruebas sin jugadores)")]
    [SerializeField] private bool useMockPlayers = true;
    [SerializeField] private int mockPlayersCount = 3;
    [SerializeField] private string mockNamePrefix = "Compañero ";

    private readonly List<PlayerCardUI> _cards = new();
    private string _localUserId;
    private string _matchId;

    private void Awake()
    {
#if PHOTON_UNITY_NETWORKING
        _localUserId = PhotonNetwork.LocalPlayer?.UserId
                       ?? PhotonNetwork.LocalPlayer?.ActorNumber.ToString();
        _matchId = $"{matchIdPrefix}-{PhotonNetwork.CurrentRoom?.Name}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
#else
        _localUserId = SystemInfo.deviceUniqueIdentifier;
        _matchId = $"{matchIdPrefix}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
#endif
    }

    private void OnEnable()
    {
        BuildList();
        UpdateCounterLabel();
        btnEnviar.onClick.AddListener(OnClickEnviar);
        btnOmitir.onClick.AddListener(OnClickOmitir);
    }

    private void OnDisable()
    {
        btnEnviar.onClick.RemoveListener(OnClickEnviar);
        btnOmitir.onClick.RemoveListener(OnClickOmitir);
    }

    private void BuildList()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
        _cards.Clear();

        if (useMockPlayers)
        {
            for (int i = 1; i <= Mathf.Max(0, mockPlayersCount); i++)
            {
                var go = Instantiate(playerCardPrefab, content);
                var ui = go.GetComponent<PlayerCardUI>();
                ui.Bind($"mock_{i}", $"{mockNamePrefix}{i}");
                _cards.Add(ui);
            }
            return;
        }

#if PHOTON_UNITY_NETWORKING
        foreach (var p in PhotonNetwork.PlayerList)
        {
            var pId = p.UserId ?? p.ActorNumber.ToString();
            if (pId == _localUserId) continue; // no me endoso a mí mismo

            var go = Instantiate(playerCardPrefab, content);
            var ui = go.GetComponent<PlayerCardUI>();
            ui.Bind(p);
            _cards.Add(ui);
        }
#endif
    }

    private int CountSelections()
    {
        int count = 0;
        foreach (var card in _cards)
            if (card.HasSelection(out _)) count++;
        return count;
    }

    private void UpdateCounterLabel()
    {
        int c = CountSelections();
        if (c > maxEndorsements)
        {
            txtCounter.text = $"Seleccionados {c}/{maxEndorsements} (exceso)";
            txtCounter.color = Color.red;
            btnEnviar.interactable = false;
        }
        else
        {
            txtCounter.text = $"{c}/{maxEndorsements} seleccionados";
            txtCounter.color = Color.white;
            btnEnviar.interactable = true;
        }
    }

    private void LateUpdate() => UpdateCounterLabel();

    // ===================== BOTÓN ENVIAR =====================

    private void OnClickEnviar()
    {
        int c = CountSelections();
        if (c > maxEndorsements) return;

        // Deshabilitar botones para evitar doble click
        btnEnviar.interactable = false;
        btnOmitir.interactable = false;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // 1) Endorsements
        var endorseList = new List<EndorsementPayload>();
        foreach (var card in _cards)
        {
            if (card.HasSelection(out var type))
            {
                endorseList.Add(new EndorsementPayload
                {
                    matchId = _matchId,
                    giverUserId = _localUserId,
                    receiverUserId = card.ReceiverUserId,
                    type = type,
                    unixTime = now
                });
            }
        }

        // 2) Compatibilidad
        var compatList = new List<CompatibilityVotePayload>();
        foreach (var card in _cards)
        {
            if (card.PlayAgainSelected)
            {
                compatList.Add(new CompatibilityVotePayload
                {
                    matchId = _matchId,
                    voterUserId = _localUserId,
                    targetUserId = card.ReceiverUserId,
                    wouldPlayAgain = true,
                    unixTime = now
                });
            }
        }

        // Si no hay nada, igual puedes mandar inferencia y luego ir al lobby
        if (endorseList.Count == 0 && compatList.Count == 0)
        {
            TrySendInference();
            StartCoroutine(CoReturnToLobbyAfterSends(waitForUploads: true));
            return;
        }

        // Asegurar uploader de endorsements en escena
        if (EndorsementUploader.Instance == null)
        {
            var go = new GameObject("EndorsementUploader");
            go.AddComponent<EndorsementUploader>();
        }

        if (endorseList.Count > 0)
            EndorsementUploader.Instance.EnqueueAndSend(endorseList);

        if (compatList.Count > 0)
            EndorsementUploader.Instance.EnqueueAndSendCompatibility(compatList);

        // Mandar también la inferencia del KPI State Machine
        TrySendInference();

        // Ahora esperamos a que terminen uploads (o timeout) y regresamos al lobby
        StartCoroutine(CoReturnToLobbyAfterSends(waitForUploads: true));
    }

    private void TrySendInference()
    {
        if (InferenceUploader.Instance == null)
        {
            var go = new GameObject("InferenceUploader");
            go.AddComponent<InferenceUploader>();
        }

        InferenceUploader.Instance.SendInference(_matchId);
    }

    // ===================== BOTÓN OMITIR =====================

    private void OnClickOmitir()
    {
        btnEnviar.interactable = false;
        btnOmitir.interactable = false;

        // No mandamos nada, solo regresamos al lobby.
        StartCoroutine(CoReturnToLobbyAfterSends(waitForUploads: false));
    }

    // ===================== CIERRE + REGRESO AL LOBBY =====================

    private System.Collections.IEnumerator CoReturnToLobbyAfterSends(bool waitForUploads)
    {
        if (waitForUploads)
        {
            float timeout = 10f;   // segundo de seguridad
            float t = 0f;

            while (HasPendingUploads() && t < timeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        CloseScreen();
        GoBackToLobby();
    }

    private bool HasPendingUploads()
    {
        bool pendingEndorse = false;
        bool pendingInference = false;

        if (EndorsementUploader.Instance != null)
        {
            pendingEndorse = EndorsementUploader.Instance.HasAnyPendingUploads;
        }

        if (InferenceUploader.Instance != null)
        {
            pendingInference = InferenceUploader.Instance.IsUploadingInference;
        }

        return pendingEndorse || pendingInference;
    }

    private void CloseScreen()
    {
        gameObject.SetActive(false);
    }

    private void GoBackToLobby()
    {
#if PHOTON_UNITY_NETWORKING
        // En online, normalmente dejas que el Master cambie de escena.
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(lobbySceneName);
        }
        else if (!PhotonNetwork.InRoom)
        {
            SceneManager.LoadScene(lobbySceneName);
        }
#else
        SceneManager.LoadScene(lobbySceneName);
#endif
    }

    // (LogPlayAgainVotes se puede quedar igual si lo usas para debug)
}
