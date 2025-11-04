using Photon.Pun;
using Photon.Voice.Unity;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    public TMP_InputField createInput;
    public TMP_InputField joinInput;

    public GameObject botonCrearUnirSalas;

    public GameObject uiCrearSalas;

    private void Start()
    {
        botonCrearUnirSalas.SetActive(false);
        uiCrearSalas.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ActivarBoton();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DesactivarBoton();
        }
    }
    public void ActivarBoton()
    {
        botonCrearUnirSalas.SetActive(true);
    }

    public void DesactivarBoton()
    {
        botonCrearUnirSalas.SetActive(false);
    }

    public void AbrirMenu()
    {
        uiCrearSalas.SetActive(true);
    }

    public void CerrarMenu()
    {
        uiCrearSalas.SetActive(false);
    }

    public void CreateRoom()
    {
        NetworkManager.Instance.CrearSala(createInput.text);
    }
    public void JoinRoom()
    {
        NetworkManager.Instance.UnirSala(joinInput.text);
    }

    
    

}
