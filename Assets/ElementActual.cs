using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class ElementActual : MonoBehaviourPun
{
    [SerializeField]
    Magic magicScript;
    
    public GameObject uiElement;
    public RawImage imgElement;
    private string pathFolder = "Elements/Images/";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        // Turn off the panel with the element if is not the player
        if (!photonView.IsMine)
        {
            uiElement.SetActive(false);
            return;
        }
        // Turn on the panel with the element if is the player
        uiElement.SetActive(true);
        // If the script Magic.cs exist, upload the element image.
        if (magicScript != null) 
            UploadElementImage(magicScript.elementoActual);
    }

    void UploadElementImage(string nameElement)
    {
        Debug.Log("MAGIC ASSIGN: " + nameElement);
        Texture2D texture = Resources.Load<Texture2D>(pathFolder+nameElement);
        if (texture == null) return;
        imgElement.texture = texture;
    }

    
}
