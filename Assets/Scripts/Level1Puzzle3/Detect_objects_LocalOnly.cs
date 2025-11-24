using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Detect_objects_LocalOnly : MonoBehaviour
{
    [Header("Rango y objetivo a detectar")]
    public float radioDeteccion = 5f;
    public string tagObjetivo = "Player";
    public string escenaDeterminada;
    public Vector3 offset = new Vector3(0, 4f, 0);

    [Header("UI")]
    public GameObject[] ui_oculto;
    public KeyCode teclaAbrir = KeyCode.E;

    private GameObject jugadorLocal;
    private bool jugadorCerca = false;
    private bool estabaCerca = false;
    private bool menuOpen = false;

    private string LoopId => $"ui_prox_{GetInstanceID()}";
    private SfxAreaOverride areaOverride;

    void Awake()
    {
        if (ui_oculto != null)
            foreach (GameObject ui in ui_oculto)
                if (ui) ui.SetActive(false);

        escenaDeterminada = SceneManager.GetActiveScene().name;
        EnsureAreaOverride();
        ApplyAreaFromRadio();
    }

    void Update()
    {
        if (string.IsNullOrEmpty(escenaDeterminada)) return;

        bool cercaAhora = DetectarJugadorLocal();
        Vector3 centro = transform.position + offset;

        seleccionAccion(escenaDeterminada);

        // AUDIO (same as your original)
        if (menuOpen)
        {
            SoundManager.Instance?.StopLoop(LoopId);
        }
        else
        {
            if (cercaAhora && !estabaCerca)
            {
                // entered range
            }
            else if (!cercaAhora && estabaCerca)
            {
                SoundManager.Instance?.StopLoop(LoopId);
            }

            if (cercaAhora)
                SoundManager.Instance?.SetLoopPosition(LoopId, centro);
        }

        estabaCerca = jugadorCerca = cercaAhora;
    }

    // --------- THIS IS THE ONLY IMPORTANT CHANGE ----------
    bool DetectarJugadorLocal()
    {
        Vector3 centro = transform.position + offset;
        Collider[] objetos = Physics.OverlapSphere(centro, radioDeteccion);

        jugadorLocal = null;

        foreach (Collider col in objetos)
        {
            if (!col.CompareTag(tagObjetivo)) continue;

            PhotonView pv = col.GetComponent<PhotonView>();
            if (!pv || !pv.IsMine) continue;   // <-- 🔥 Only local player's collider counts

            jugadorLocal = col.gameObject;
            return true;
        }
        return false;
    }
    // -------------------------------------------------------

    void seleccionAccion(string nombreEscena)
    {
        switch (gameObject.tag)
        {
            case "Libro":
                LibroLobby(nombreEscena);
                break;
            case "Armario":
                ArmarioLobby(nombreEscena);
                break;
        }
    }

    void LibroLobby(string nombreEscena)
    {
        if (nombreEscena != escenaDeterminada) return;

        if (ui_oculto[0])
            ui_oculto[0].SetActive(jugadorCerca);

        if (Input.GetKeyDown(teclaAbrir) && jugadorLocal)
        {
            menuOpen = !menuOpen;

            foreach (var ui in ui_oculto)
                if (ui) ui.SetActive(menuOpen);
        }
    }

    void ArmarioLobby(string nombreEscena)
    {
        if (nombreEscena != escenaDeterminada) return;

        if (ui_oculto.Length > 0)
            ui_oculto[0].SetActive(jugadorCerca);

        if (Input.GetKeyDown(teclaAbrir) && jugadorLocal)
        {
            menuOpen = !menuOpen;

            for (int i = 1; i < ui_oculto.Length; i++)
                ui_oculto[i]?.SetActive(menuOpen);
        }
    }

    // ---------- Audio helpers ----------
    void EnsureAreaOverride()
    {
        if (!areaOverride) areaOverride = GetComponent<SfxAreaOverride>();
        if (!areaOverride) areaOverride = gameObject.AddComponent<SfxAreaOverride>();
    }

    void ApplyAreaFromRadio()
    {
        if (!areaOverride) return;
        areaOverride.spatialBlend = 1f;
        areaOverride.minDistance = Mathf.Max(0.1f, radioDeteccion * 0.25f);
        areaOverride.maxDistance = Mathf.Max(areaOverride.minDistance + 0.1f, radioDeteccion);
    }
}
