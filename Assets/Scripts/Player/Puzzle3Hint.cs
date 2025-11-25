using UnityEngine;
using TMPro;

public class Puzzle3MessageUI : MonoBehaviour
{
    private TextMeshProUGUI text;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        HideMessage();
    }

    public void ShowMessage(string msg)
    {
        text.text = msg;
        text.alpha = 1f;
    }

    public void HideMessage()
    {
        text.alpha = 0f;
    }
}
