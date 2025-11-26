using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class PlayerNameListUI : MonoBehaviourPunCallbacks
{
    [Header("Referencias UI")]
    public Transform contenedorJugadores;
    public GameObject prefabJugadorItem;
    public TextMeshProUGUI nombreSala;

    private readonly Dictionary<Player, TMP_Text> itemsUI = new();
    private PlayerName jugadorLocal;

    public string jugadorLocalTexto = "Tú";
    private string salaTitulo = "Sala";
    private System.Action handlerJugadoresActualizados;
    void OnEnable()
    {
        handlerJugadoresActualizados = () =>
        {
            if (jugadorLocal != null)
                ActualizarListaUI();
        };

        PlayerName.OnJugadoresActualizados += handlerJugadoresActualizados;
    }

    void OnDisable()
    {
        PlayerName.OnJugadoresActualizados -= handlerJugadoresActualizados;
    }

    void Start()
    {
        

        if (PhotonNetwork.InRoom)
            nombreSala.text = $"{salaTitulo}: {PhotonNetwork.CurrentRoom.Name}";
        else
            nombreSala.text = "Sin sala";

        // Buscar jugador local
        foreach (var kvp in PlayerName.jugadoresActivos)
        {
            if (kvp.Value.photonView.IsMine)
            {
                jugadorLocal = kvp.Value;
                break;
            }
        }

        // Si aún no está listo, reintentar
        if (jugadorLocal == null)
            InvokeRepeating(nameof(RevisarJugadorLocal), 0.2f, 0.2f);
        else
            ActualizarListaUI();

        InvokeRepeating(nameof(ActualizarDistancias), 0.5f, 0.5f);
    }

    void RevisarJugadorLocal()
    {
        foreach (var kvp in PlayerName.jugadoresActivos)
        {
            if (kvp.Value.photonView.IsMine)
            {
                jugadorLocal = kvp.Value;
                CancelInvoke(nameof(RevisarJugadorLocal));
                ActualizarListaUI();
                break;
            }
        }
    }

    void ActualizarListaUI()
    {
        if (jugadorLocal == null) return;

        // 1. Guardar los hijos en una lista temporal
        List<Transform> hijos = new List<Transform>();
        foreach (Transform child in contenedorJugadores)
            hijos.Add(child);

        // 2. Destruir los hijos desde la lista
        foreach (var child in hijos)
            Destroy(child.gameObject);

        itemsUI.Clear();

        // Crear lista actualizada
        foreach (var kvp in PlayerName.jugadoresActivos)
        {
            var jugador = kvp.Key;
            var info = kvp.Value;

            GameObject item = Instantiate(prefabJugadorItem, contenedorJugadores);
            TMP_Text texto = item.GetComponentInChildren<TMP_Text>();

            texto.text = info.photonView.IsMine
                ? $"{info.playerName} ({jugadorLocalTexto})"
                : $"{info.playerName}";

            itemsUI[jugador] = texto;
        }
    }
    void ActualizarDistancias()
    {
        if (jugadorLocal == null) return;

        foreach (var kvp in PlayerName.jugadoresActivos)
        {
            var jugador = kvp.Key;
            var info = kvp.Value;

            if (!itemsUI.ContainsKey(jugador)) continue;

            TMP_Text texto = itemsUI[jugador];
            if (info.photonView.IsMine)
                texto.text = $"{info.playerName} ({jugadorLocalTexto})";
            else
            {
                float distancia = Vector3.Distance(jugadorLocal.transform.position, info.transform.position);
                texto.text = $"{info.playerName} — {distancia:F1} m — {PhotonNetwork.GetPing()} ms";
            }
        }
    }
}
