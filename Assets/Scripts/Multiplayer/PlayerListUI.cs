using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerNameListUI : MonoBehaviourPunCallbacks
{
    // ----------- SINGLETON PARA EVITAR DUPLICADOS ----------------
    private static PlayerNameListUI instancia;

    void Awake()
    {
        if (instancia != null)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    // ------------------------ UI -------------------------------
    [Header("Referencias UI")]
    public Transform contenedorJugadores;
    public GameObject prefabJugadorItem;
    public TextMeshProUGUI nombreSala;

    private readonly Dictionary<Player, TMP_Text> itemsUI = new();
    private PlayerName jugadorLocal;

    public string jugadorLocalTexto = "Tú";
    private string salaTitulo = "Sala";

    // ---------------- EVENTOS ---------------------------
    void OnEnable()
    {
        PlayerName.OnJugadoresActualizados += ActualizarListaUI;
    }

    void OnDisable()
    {
        PlayerName.OnJugadoresActualizados -= ActualizarListaUI;
    }

    // ---------------- START ------------------------------
    void Start()
    {
        ActualizarNombreSala();
        BuscarJugadorLocal();

        InvokeRepeating(nameof(ActualizarDistancias), 0.5f, 0.5f);
    }

    private void ActualizarNombreSala()
    {
        nombreSala.text = PhotonNetwork.InRoom
            ? $"{salaTitulo}: {PhotonNetwork.CurrentRoom.Name}"
            : "Sin sala";
    }

    private void BuscarJugadorLocal()
    {
        foreach (var kvp in PlayerName.jugadoresActivos)
        {
            if (kvp.Value.photonView.IsMine)
            {
                jugadorLocal = kvp.Value;
                break;
            }
        }
    }

    // ---------------- LISTA DE JUGADORES ----------------------
    private void ActualizarListaUI()
    {
        if (jugadorLocal == null)
            BuscarJugadorLocal();

        if (jugadorLocal == null) return;

        // LIMPIAR UI ANTERIOR
        foreach (Transform niño in contenedorJugadores)
            Destroy(niño.gameObject);

        itemsUI.Clear();

        // CREAR LISTA NUEVA
        foreach (var kvp in PlayerName.jugadoresActivos)
        {
            Player jugador = kvp.Key;
            PlayerName info = kvp.Value;

            GameObject item = Instantiate(prefabJugadorItem, contenedorJugadores);
            TMP_Text texto = item.GetComponentInChildren<TMP_Text>();

            texto.text = info.photonView.IsMine
                ? $"{info.playerName} ({jugadorLocalTexto})"
                : info.playerName;

            itemsUI.Add(jugador, texto);
        }
    }

    // ---------------- DISTANCIAS ---------------------
    private void ActualizarDistancias()
    {
        if (jugadorLocal == null) return;

        foreach (var kvp in PlayerName.jugadoresActivos)
        {
            Player jugador = kvp.Key;
            PlayerName info = kvp.Value;

            if (!itemsUI.ContainsKey(jugador))
                continue;

            TMP_Text texto = itemsUI[jugador];

            if (info.photonView.IsMine)
            {
                texto.text = $"{info.playerName} ({jugadorLocalTexto})";
            }
            else
            {
                float distancia = Vector3.Distance(
                    jugadorLocal.transform.position,
                    info.transform.position
                );

                texto.text =
                    $"{info.playerName} — {distancia:F1} m — {PhotonNetwork.GetPing()} ms";
            }
        }
    }
}
