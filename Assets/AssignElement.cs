using System.Runtime.CompilerServices;
using UnityEngine;

public class AssignElement : MonoBehaviour
{
    // Element in the array
    /*
     0 - Fire
     1 - Water
     2 - Earth
     3 - Wind
     */
    public Material[] elements;
    
    private Renderer rend;

    public GameObject quadElement;
    
    void Start()
    {
        rend = quadElement.GetComponent<Renderer>();
        SelectElement();
    }

    void SelectElement()
    {
        string actualTag = gameObject.tag;
        
        switch (actualTag) {
            case "Fire":
                rend.material = elements[0];
                break;
            case "Water":
                rend.material = elements[1];
                break;
            case "Earth":
                rend.material = elements[2];
                break;
            case "Wind":
                rend.material = elements[3];
                break;
        }
    }
}
