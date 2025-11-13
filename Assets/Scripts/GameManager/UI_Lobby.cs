using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Lobby : MonoBehaviour
{
    public TMP_InputField createInput;
    public TMP_InputField unirInput;
    public Button crear;
    public Button unir;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (crear!=null) crear.onClick.AddListener(CreateRoom);
        if (unir != null) unir.onClick.AddListener(JoinRoom);
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(createInput.text)) return;
        NetworkManager.Instance.CrearSala(createInput.text);
    }

    public void JoinRoom()
    {
        if (string.IsNullOrEmpty(unirInput.text)) return;
        Debug.Log(unirInput.text);
        NetworkManager.Instance.UnirSala(unirInput.text);
    }
}
