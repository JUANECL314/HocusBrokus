using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Temp_MenuUI : MonoBehaviour
{
    public GameObject MenuUILevels;
    public float radioDeteccion = 5f;
    public Button boton;
    public KeyCode teclaAbrir = KeyCode.R;
    public Vector3 offset = new Vector3(0, 4f, 0);
    private GameObject jugador;
    public string tagObjetivo = "Player";
    private void Awake()
    {
        MenuUILevels.SetActive(false);
    }
    private void Start()
    {
        if (MenuUILevels.activeSelf) MenuUILevels.SetActive(false);
        if(boton != null) boton.onClick.AddListener(IrPartida);
    }
    private void Update()
    {
        bool cercaMaster = DetectarMaster();
        if (Input.GetKeyDown(teclaAbrir) && !MenuUILevels.activeSelf && cercaMaster)
        {
            MenuUILevels.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetKeyDown(teclaAbrir) && MenuUILevels.activeSelf && cercaMaster)
        {
            MenuUILevels.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
    }

    public void IrPartida()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        string escenaActual = SceneManager.GetActiveScene().name;
        string escenaCambiar = "";
        Debug.Log(escenaCambiar + escenaActual);
        if (escenaActual == "TownRoom") escenaCambiar = "CavePuzzle1";
        if (escenaActual == "CavePuzzle1") escenaCambiar = "CavePuzzle2";
        if (escenaActual == "CavePuzzle2") escenaCambiar = "CavePuzzle3";
        if (escenaActual == "CavePuzzle3") escenaCambiar = "TownRoom";
        PhotonNetwork.LoadLevel(escenaCambiar);

    }

    bool DetectarMaster ()
    {
        if (!PhotonNetwork.IsMasterClient) return false;
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
}
