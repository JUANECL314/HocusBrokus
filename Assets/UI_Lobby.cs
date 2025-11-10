using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Lobby : MonoBehaviour
{
    public TMP_InputField createInput;

    public Button crear;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (crear!=null) crear.onClick.AddListener(CreateRoom);
    }

    public void CreateRoom()
    {
        NetworkManager.Instance.CrearSala(createInput.text);
    }
}
