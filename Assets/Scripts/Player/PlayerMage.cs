using UnityEngine;

public class PlayerMage: MonoBehaviour
{
    public string name;
    public int velocityMov;
    public Magic element;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        element.elementDescription();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
