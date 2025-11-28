using UnityEngine;

public class CounterCode : MonoBehaviour
{
    [Header("Contadores")]
    public int counter = 0;
    public int total = 1;
    
    public bool complete = false;

    public string tagName;

    void Start()
    {
        tagName = gameObject.tag;
    }
    
}
