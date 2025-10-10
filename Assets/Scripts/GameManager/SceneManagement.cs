using Photon.Pun;
using UnityEngine;

public class SceneManagement : MonoBehaviour
{

    [SerializeField]
    GameObject inicio;
    [SerializeField]
    GameObject inicioSesion;
    bool esIniciarSesion = true;

    void Start()
    {
        Menu();
    }
    public void CaveLevel()
    {
        PhotonNetwork.LoadLevel("Cave");
    }

    public void IniciarSesion()
    {
        inicio.SetActive(!esIniciarSesion);
        inicioSesion.SetActive(esIniciarSesion);
    }

    public void Menu()
    {
        inicio.SetActive(esIniciarSesion);
        inicioSesion.SetActive(!esIniciarSesion);
    }

    public void Salir()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit(); 
        #endif
    }
    
}
