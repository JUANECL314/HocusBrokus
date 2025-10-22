using Photon.Pun;
using UnityEngine;

public class Elements : MonoBehaviour
{
    public enum ElementType { Water, Earth, Fire, Wind, Other }

    public string idName;
    public int velocityMov;
    public int weight;

    // select the element type in the inspector
    public ElementType elementType = ElementType.Other;

    
}
