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
    private bool jugadorCerca = false;   // estado actual (audio/UI)
    private bool estabaCerca = false;    // estado previo (para transición)
    private bool menuOpen = false;       // <<<<<< NUEVO: estado de menú

    // ---------- Audio (SoundManager) ----------
    private string LoopId => $"ui_prox_{GetInstanceID()}";
    [Tooltip("Sonido one-shot al entrar al rango")]
    public SfxKey enterKey = SfxKey.UIProximityEnter;
    [Tooltip("Sonido loop mientras permanezca en el rango")]
    public SfxKey loopKey = SfxKey.UIProximityLoop;

    // override 3D por objeto (maxDistance = radioDeteccion)
    private SfxAreaOverride areaOverride;

    void Awake()
    {
        // Oculta UI al inicio
        if (ui_oculto != null)
            foreach (GameObject ui in ui_oculto) if (ui) ui.SetActive(false);

        escenaDeterminada = SceneManager.GetActiveScene().name;

        // Asegurar/ajustar override de área 3D
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

        // 1) DETECCIÓN (audio/UI)
        bool cercaAhora = DetectarJugador(); // no altera UI, solo dice si hay player en rango
        Vector3 centro = transform.position + offset;

        // 2) LÓGICA DE UI (puede cambiar menuOpen este frame)
        seleccionAccion(escenaDeterminada);

        // 3) AUDIO — tener en cuenta menuOpen
        if (menuOpen)
        {
            // con menú abierto: garantizar loop detenido y no arrancar nada
            SoundManager.Instance?.StopLoop(LoopId);
        }
        else
        {
            // Transiciones de audio solo si NO hay menú
            if (cercaAhora && !estabaCerca)
            {
                // Entró al rango
                SoundManager.Instance?.Play(enterKey, centro);
                SoundManager.Instance?.StartLoop(LoopId, loopKey, centro);
            }
            else if (!cercaAhora && estabaCerca)
            {
                // Salió del rango
                SoundManager.Instance?.StopLoop(LoopId);
            }

            // Si está dentro, mantener loop anclado al centro del rango
            if (cercaAhora)
                SoundManager.Instance?.SetLoopPosition(LoopId, centro);
        }

        estabaCerca = jugadorCerca = cercaAhora;
    }

    // ---------- Detección genérica ----------
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
            default:
                break;
        }
    }

    void LibroLobby(string nombreEscena)
    {
        if (nombreEscena != escenaDeterminada || ui_oculto.Length != longitudArreglo) return;

        // Prompt visible según proximidad
        if (ui_oculto[0] != null) ui_oculto[0].SetActive(jugadorCerca);

        // ABRIR
        if (Input.GetKeyDown(teclaAbrir) &&
            !ui_oculto[1].activeSelf && !ui_oculto[2] &&
            ui_oculto[0].activeSelf)
        {
            if (!jugador) return;
            var dbg = jugador.GetComponent<FreeFlyDebug>();
            if (dbg) dbg.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EjecutarAccion(ui_oculto[1], true);
            

            // <<<<<< MUTE AL ABRIR
            menuOpen = true;
            SoundManager.Instance?.StopLoop(LoopId);
        }
        // CERRAR
        else if (Input.GetKeyDown(teclaAbrir) &&
                 ui_oculto[1].activeSelf && !ui_oculto[2] && ui_oculto[0].activeSelf)
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

            // <<<<<< REANUDAR si sigues dentro
            menuOpen = false;
            if (jugadorCerca)
            {
                var centro = transform.position + offset;
                SoundManager.Instance?.StartLoop(LoopId, loopKey, centro);
            }
        }
    }

    void ArmarioLobby(string nombreEscena)
    {
        if (nombreEscena != escenaDeterminada || ui_oculto.Length != longitudArreglo) return;

        if (ui_oculto[0] != null) ui_oculto[0].SetActive(jugadorCerca);

        // ABRIR
        if (Input.GetKeyDown(teclaAbrir) && ui_oculto[0].activeSelf && !ui_oculto[1].activeSelf)
        {
            if (!jugador) return;
            var dbg = jugador.GetComponent<FreeFlyDebug>();
            if (dbg) dbg.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EjecutarAccion(ui_oculto[1], true);

            // <<<<<< MUTE AL ABRIR
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

            // <<<<<< REANUDAR si sigues dentro
            menuOpen = false;
            if (jugadorCerca)
            {
                var centro = transform.position + offset;
                SoundManager.Instance?.StartLoop(LoopId, loopKey, centro);
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
        areaOverride.spatialBlend = 1f; // 3D
        areaOverride.minDistance = Mathf.Max(0.1f, radioDeteccion * 0.25f);
        areaOverride.maxDistance = Mathf.Max(areaOverride.minDistance + 0.1f, radioDeteccion);
    }
}
