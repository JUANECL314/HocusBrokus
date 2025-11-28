using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class PlayerNameListUI : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public Transform contenedorJugadores;
    public GameObject prefabJugadorItem;
    public TextMeshProUGUI nombreSala;

    [Header("Textos")]
    public string jugadorLocalTexto = "TÚ";
    public string salaTitulo = "Sala";

    [Header("Debug")]
    [SerializeField] private bool logDebug = true;

    private readonly Dictionary<Player, TMP_Text> itemsUI = new();

    void OnEnable()
    {
        PlayerName.OnJugadoresActualizados += HandleJugadoresActualizados;
    }

    void OnDisable()
    {
        PlayerName.OnJugadoresActualizados -= HandleJugadoresActualizados;
    }

    void Start()
    {
        if (PhotonNetwork.InRoom)
            nombreSala.text = $"{salaTitulo}: {PhotonNetwork.CurrentRoom.Name}";
        else
            nombreSala.text = "Sin sala";

        if (logDebug)
        {
            Debug.Log($"[PlayerNameListUI] Start en cliente {PhotonNetwork.LocalPlayer.ActorNumber}, " +
                      $"Nick={PhotonNetwork.NickName}");
        }

        ActualizarListaUI();
        InvokeRepeating(nameof(ActualizarDistancias), 0.5f, 0.5f);
    }

    private void HandleJugadoresActualizados()
    {
        if (logDebug)
        {
            Debug.Log($"[PlayerNameListUI] HandleJugadoresActualizados en cliente {PhotonNetwork.LocalPlayer.ActorNumber}. " +
                      $"Total jugadoresActivos = {PlayerName.jugadoresActivos.Count}");
        }
        ActualizarListaUI();
    }

    private void ActualizarListaUI()
    {
        // 1. limpiar hijos del contenedor
        List<Transform> hijos = new();
        foreach (Transform child in contenedorJugadores)
            hijos.Add(child);

        foreach (var child in hijos)
            Destroy(child.gameObject);

        itemsUI.Clear();

        if (PlayerName.jugadoresActivos.Count == 0)
        {
            if (logDebug)
                Debug.Log("[PlayerNameListUI] No hay jugadoresActivos aún.");
            return;
        }

        // 2. crear items ordenados por ActorNumber para que el orden sea estable
        foreach (var kvp in PlayerName.jugadoresActivos.OrderBy(p => p.Key.ActorNumber))
        {
            Player jugador = kvp.Key;
            PlayerName info = kvp.Value;

            GameObject item = Instantiate(prefabJugadorItem, contenedorJugadores);
            TMP_Text texto = item.GetComponentInChildren<TMP_Text>();

            if (info == null)
            {
                texto.text = "(desconocido)";
            }
            else
            {
                bool esLocal = jugador == PhotonNetwork.LocalPlayer;

                if (esLocal)
                    texto.text = $"{info.playerName} ({jugadorLocalTexto})";
                else
                    texto.text = $"{info.playerName}";
            }

            itemsUI[jugador] = texto;

            if (logDebug)
            {
                Debug.Log($"[PlayerNameListUI] Creo item para Player {jugador.ActorNumber} " +
                          $"name='{info?.playerName}', esLocal={jugador == PhotonNetwork.LocalPlayer}");
            }
        }
    }

    private void ActualizarDistancias()
    {
        PlayerName local = PlayerName.LocalPlayerName;
        if (local == null)
        {
            if (logDebug)
            {
                Debug.Log($"[PlayerNameListUI] ActualizarDistancias sin LocalPlayerName en cliente " +
                          $"{PhotonNetwork.LocalPlayer.ActorNumber}");
            }
            return;
        }

        foreach (var kvp in PlayerName.jugadoresActivos)
        {
            Player jugador = kvp.Key;
            PlayerName info = kvp.Value;

            if (info == null) continue;
            if (!itemsUI.TryGetValue(jugador, out TMP_Text texto)) continue;

            bool esLocal = jugador == PhotonNetwork.LocalPlayer;

            if (esLocal)
            {
                texto.text = $"{info.playerName} ({jugadorLocalTexto})";
            }
            else
            {
                float distancia = Vector3.Distance(local.transform.position, info.transform.position);
                int ping = PhotonNetwork.GetPing();
                texto.text = $"{info.playerName} — {distancia:F1} m — {ping} ms";
            }
        }
    }
}
