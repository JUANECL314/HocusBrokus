using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class PlayerNameListUI : MonoBehaviourPunCallbacks
{
    [Header("Referencias UI")]
    public Transform contenedorJugadores;   // Donde se instancian los nombres
    public GameObject prefabJugadorItem;    // Prefab con un TMP_Text dentro

    private readonly Dictionary<Player, TMP_Text> itemsUI = new();
    private PlayerName jugadorLocal;
    public string jugadorLocalTexto = "Tú";
    void Start()
    {
        // Solo el jugador local controla su UI
        if (!photonView.IsMine)
        {
            gameObject.SetActive(false);
            return;
        }

        // Buscar al jugador local
        foreach (var kvp in PlayerName.jugadoresActivos)
        {
            if (kvp.Value.photonView.IsMine)
            {
                jugadorLocal = kvp.Value;
                break;
            }
        }

        // Crear los textos iniciales
        ActualizarListaUI();

        // Actualizar cada 0.5 segundos
        InvokeRepeating(nameof(ActualizarDistancias), 0.5f, 0.5f);
    }

    void ActualizarListaUI()
    {
        // Limpiar
        foreach (var t in itemsUI.Values)
            Destroy(t.gameObject);

        itemsUI.Clear();

        // Crear un TMP_Text para cada jugador activo
        foreach (var kvp in PlayerName.jugadoresActivos)
        {
            var jugador = kvp.Key;
            var info = kvp.Value;

            GameObject item = Instantiate(prefabJugadorItem, contenedorJugadores);
            TMP_Text texto = item.GetComponent<TMP_Text>();

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
            {
                texto.text = $"{info.playerName} ({jugadorLocalTexto})";
            }
            else
            {
                float distancia = Vector3.Distance(jugadorLocal.transform.position, info.transform.position);
                texto.text = $"{info.playerName} — {distancia:F1} m";
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        ActualizarListaUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ActualizarListaUI();
    }
}
