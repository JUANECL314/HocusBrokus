using UnityEngine;

public class UIControllerPuzzle2 : MonoBehaviour
{
    public static UIControllerPuzzle2 Instance;

    public GameObject panelUI;
    private ButtonActivation currentButton;

    void Awake()
    {
        Instance = this;
    }

    public void ShowButtonUI(bool state, ButtonActivation btn)
    {
        if (state)
        {
            currentButton = btn;
            panelUI.SetActive(true);
        }
        else if (currentButton == btn)
        {
            panelUI.SetActive(false);
            currentButton = null;
        }
    }
}
