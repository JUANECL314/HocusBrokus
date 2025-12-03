using TMPro;
public class MovementPOIWithText : MovementPOIBase
{
    public TMP_Text textElement;
    public string text;

    protected override void OnStart()
    {
        if (textElement != null)
            textElement.text = text;
    }
}
