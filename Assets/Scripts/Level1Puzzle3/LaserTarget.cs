using UnityEngine;

public class LaserTarget : MonoBehaviour
{
    public bool isActivated = false;

    public void Activate()
    {
        if (!isActivated)
        {
            isActivated = true;
            Debug.Log("Puzzle Complete!");
            // Add effects, sounds, etc.
        }
    }
}
