using System;
using UnityEngine;
public class DoorController : MonoBehaviour, IObserver
{
    public ButtonActivation[] mainButtons; // Solo los botones principales
    private bool doorOpened = false;

    void Start()
    {
        foreach (var b in mainButtons)
            b.AddObserver(this);
    }

    public void OnNotify(int id, bool state)
    {
        if (!doorOpened && AllPressed())
        {
            OpenDoor();
            doorOpened = true;
        }
    }

    private bool AllPressed()
    {
        foreach (var b in mainButtons)
            if (!b.isEnabled || !b.isPressed)
                return false;
        return true;
    }

    private void OpenDoor()
    {
        
        gameObject.SetActive(true);
    }
}