using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Detect_objects : MonoBehaviour
{
    [Header("Rango y objetivo a detectar")]
    public float radioDeteccion = 5f;
    public string tagObjetivo = "Player";
    public string escenaDeterminada;
    public Vector3 offset = new Vector3(0, 4f, 0);

    [Header("UI")]
    public GameObject[] ui_oculto;
    public KeyCode teclaAbrir = KeyCode.E;
    public int longitudArreglo = 6;

    // Estado
    private GameObject jugador;
    private bool jugadorCerca = false;
    private bool estabaCerca = false;
    private bool menuOpen = false; // NUEVO: estado de menú

    // ---------- Audio (SoundManager) ----------
    private string LoopId => $"ui_prox_{GetInstanceID()}";

    // override 3D por objeto
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

    void OnValidate()
    {
        if (radioDeteccion < 0f) radioDeteccion = 0f;
        EnsureAreaOverride();
        ApplyAreaFromRadio();
    }

    void Update()
    {
        if (string.IsNullOrEmpty(escenaDeterminada)) return;

        // 1) Detección
        bool cercaAhora = DetectarJugador();
        Vector3 centro = transform.position + offset;

        // 2) Lógica UI
        seleccionAccion(escenaDeterminada);

        // 3) Audio (considera menuOpen)
        if (menuOpen)
        {
            SoundManager.Instance?.StopLoop(LoopId);
        }
        else
        {
            if (cercaAhora && !estabaCerca)
            {
                //SoundManager.Instance?.Play(enterKey, centro);
                //SoundManager.Instance?.StartLoop(LoopId, loopKey, centro);
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

    // ---------- Detección ----------
    bool DetectarJugador()
    {
        Vector3 centroDeteccion = transform.position + offset;
        Collider[] objetos = Physics.OverlapSphere(centroDeteccion, radioDeteccion);

        jugador = null;

        foreach (Collider col in objetos)
        {
            if (col.CompareTag(tagObjetivo))
            {
                jugador = col.gameObject;
                return true;
            }
        }
        return false;
    }

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
            case "Player":
                //PortalLobby(nombreEscena);
                break;
        }
    }

    void PortalLobby(string nombreEscena) { }

    void LibroLobby(string nombreEscena)
    {
        if (nombreEscena != escenaDeterminada || ui_oculto.Length != longitudArreglo) return;

        if (ui_oculto[0] != null)
            ui_oculto[0].SetActive(jugadorCerca);

        // ABRIR
        if (Input.GetKeyDown(teclaAbrir) && !ui_oculto[1].activeSelf && ui_oculto[0].activeSelf)
        {
            if (!jugador) return;

            var dbg = jugador.GetComponent<FreeFlyDebug>();
            if (dbg) dbg.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EjecutarAccion(ui_oculto[1], true);
            EjecutarAccion(ui_oculto[2], true);

            menuOpen = true;
            SoundManager.Instance?.StopLoop(LoopId);
        }
        // CERRAR
        else if (Input.GetKeyDown(teclaAbrir) && ui_oculto[1].activeSelf && ui_oculto[0].activeSelf)
        {
            for (int i = 1; i < ui_oculto.Length; i++)
                EjecutarAccion(ui_oculto[i], false);

            if (jugador)
            {
                var dbg = jugador.GetComponent<FreeFlyDebug>();
                if (dbg) dbg.enabled = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            menuOpen = false;

            if (jugadorCerca)
            {
                var centro = transform.position + offset;
                //SoundManager.Instance?.StartLoop(LoopId, loopKey, centro);
            }
        }
    }

    void ArmarioLobby(string nombreEscena)
    {
        if (nombreEscena != escenaDeterminada || ui_oculto.Length != longitudArreglo) return;

        if (ui_oculto[0] != null)
            ui_oculto[0].SetActive(jugadorCerca);

        // ABRIR
        if (Input.GetKeyDown(teclaAbrir) && ui_oculto[0].activeSelf && !ui_oculto[1].activeSelf)
        {
            if (!jugador) return;

            var dbg = jugador.GetComponent<FreeFlyDebug>();
            if (dbg) dbg.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EjecutarAccion(ui_oculto[1], true);

            menuOpen = true;
            SoundManager.Instance?.StopLoop(LoopId);
        }
        // CERRAR
        else if (Input.GetKeyDown(teclaAbrir) && ui_oculto[1].activeSelf && ui_oculto[0].activeSelf)
        {
            for (int i = 1; i < ui_oculto.Length; i++)
                EjecutarAccion(ui_oculto[i], false);

            if (jugador)
            {
                var dbg = jugador.GetComponent<FreeFlyDebug>();
                if (dbg) dbg.enabled = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            menuOpen = false;

            if (jugadorCerca)
            {
                var centro = transform.position + offset;
                //SoundManager.Instance?.StartLoop(LoopId, loopKey, centro);
            }
        }
    }

    void EjecutarAccion(GameObject ui, bool activar)
    {
        if (ui) ui.SetActive(activar);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 centroDeteccion = transform.position + offset;
        Gizmos.DrawWireSphere(centroDeteccion, radioDeteccion);
    }

    // ---------- helpers area override ----------
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
