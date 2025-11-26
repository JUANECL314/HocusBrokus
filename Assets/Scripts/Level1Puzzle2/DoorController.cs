using UnityEngine;

public class DoorController : MonoBehaviour, IObserver
{
    public ButtonActivation[] buttons;
    
    private bool[] buttonStates;

    

    void Start()
    {
        buttonStates = new bool[buttons.Length];
        foreach (var b in buttons)
        {
            b.AddObserver(this);
        }
    }

    public void OnNotify(int id, bool state)
    {
        if (id < 0 || id >= buttonStates.Length) return;

        buttonStates[id] = state;

        // si todos los botones están presionados
        if (AllPressed())
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private bool AllPressed()
    {
        for (int i = 0; i < buttonStates.Length; i++)
            if (!buttonStates[i])
                return false;

        return true;
    }
}
