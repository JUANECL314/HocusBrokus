using TMPro;
using UnityEngine;

public class AuxCriptacion : MonoBehaviour
{
    public TextMeshProUGUI texto;

    public GameObject botonDesencriptar;

    private void Awake()
    {
        texto.text = "";
    }
    public void Desencriptar()
    {
        // Enviar al backend (cola offline + reintentos)
        if (EndorsementUploader.Instance == null)
        {
            var go = new GameObject("EndorsementUploader");
            go.AddComponent<EndorsementUploader>();
        }
        //EndorsementUploader.Instance.LoadQueue();
    }   
}
