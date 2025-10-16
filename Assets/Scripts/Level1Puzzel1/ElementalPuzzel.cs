using System.Collections;
using UnityEngine;

public class ElementalPuzzle : MonoBehaviour
{
    private bool fireHit = false;
    private bool windHit = false;
    private bool puzzleActivated = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Fire"))
            fireHit = true;

        if (other.CompareTag("Wind"))
            windHit = true;

        if (fireHit && windHit && !puzzleActivated)
        {
            puzzleActivated = true;
            ActivateGears();
        }
    }

    void ActivateGears()
    {
        GameObject[] gears = GameObject.FindGameObjectsWithTag("Engranaje");
        foreach (GameObject gear in gears)
        {
            GearBehavior gb = gear.GetComponent<GearBehavior>();
            if (gb != null)
                gb.StartRotation();
        }
    }
}
