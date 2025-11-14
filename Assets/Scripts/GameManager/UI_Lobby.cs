using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Lobby : MonoBehaviour
{

    public GameObject createScene;
    public GameObject joinScene;
 
    public TMP_InputField nameRoom;
    public Button createButton;
    public Button joinButton;
    public Button createRoomButton;

    bool flag = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (createButton != null) createButton.onClick.AddListener(CreateRoomButtonPanel);
        if (joinButton != null) joinButton.onClick.AddListener(JoinRoomButtonPanel);
        if (createRoomButton != null) createRoomButton.onClick.AddListener(CreateRoomAction);
        
    }

    public void CreateRoomButtonPanel()
    {
        createScene.SetActive(flag);
        joinScene.SetActive(!flag);
 
    }

    public void JoinRoomButtonPanel()
    {
        createScene.SetActive(!flag);
        joinScene.SetActive(flag);

    }

    public void CreateRoomAction()
    {
        NetworkManager.Instance.CrearSala(nameRoom.text);
        createScene.SetActive(!flag);
        joinScene.SetActive(!flag);

    }
}
