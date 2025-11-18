using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Temp_MenuUI : MonoBehaviour
{
    public GameObject MenuUILevels;
    public Button boton;
    public KeyCode teclaAbrir = KeyCode.R;

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
        if (!PhotonNetwork.IsMasterClient) return;
        if (Input.GetKeyDown(teclaAbrir) && !MenuUILevels.activeSelf)
        {
            MenuUILevels.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetKeyDown(teclaAbrir) && MenuUILevels.activeSelf)
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
        if (escenaActual == "CavePuzzle1") escenaCambiar = "CavePuzzle3";
        if (escenaActual == "CavePuzzle3") escenaCambiar = "TownRoom";
        PhotonNetwork.LoadLevel(escenaCambiar);

    }
}
