using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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

    [Header("Mock (para pruebas sin jugadores)")]
    [SerializeField] private bool useMockPlayers = true;  // <- activa esto en el Inspector
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
        // limpiar
        foreach (Transform child in content) Destroy(child.gameObject);
        _cards.Clear();

        // 1) Si está activo el modo mock, generamos tarjetas ficticias siempre
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

        // 2) Si no hay mock, usamos Photon si está disponible
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
#else
        // 3) Sin Photon ni mock → nada (o podrías forzar una lista mínima aquí)
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

    private void OnClickEnviar()
    {
        int c = CountSelections();
        if (c == 0) { CloseScreen(); return; }
        if (c > maxEndorsements) return;

        var list = new List<EndorsementPayload>();
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var card in _cards)
            if (card.HasSelection(out var type))
                list.Add(new EndorsementPayload
                {
                    matchId = _matchId,
                    giverUserId = _localUserId,
                    receiverUserId = card.ReceiverUserId,
                    type = type,
                    unixTime = now
                });

        // Enviar al backend (cola offline + reintentos)
        if (EndorsementUploader.Instance == null)
        {
            var go = new GameObject("EndorsementUploader");
            go.AddComponent<EndorsementUploader>();
        }
        EndorsementUploader.Instance.EnqueueAndSend(list);

        CloseScreen();
    }

    private void OnClickOmitir() => CloseScreen();

    private void CloseScreen()
    {
        gameObject.SetActive(false);
        // aquí puedes cargar la pantalla de resultados/menú, etc.
    }
}
