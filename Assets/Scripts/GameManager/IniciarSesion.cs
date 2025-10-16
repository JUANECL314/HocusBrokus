using Photon.Pun;
using TMPro;
using UnityEngine;

public class IniciarSesion : MonoBehaviour
{
    public TMP_InputField player;
    public TMP_InputField password;
    public static IniciarSesion instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GuardarUsuario()
    {
        PhotonNetwork.NickName = player.text;
    }
}
