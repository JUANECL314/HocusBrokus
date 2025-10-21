using System.Collections;
using UnityEngine;

public class ElementalPuzzle : MonoBehaviour
{
    private bool fireHit = false;
    private bool windHit = false;
    private bool waterHit = false;
    private bool puzzleActivated = false;

    void OnTriggerEnter(Collider other)
    {
        // Detectar las magias que entran al trigger
        if (other.CompareTag("Fire"))
            fireHit = true;

        if (other.CompareTag("Wind"))
            windHit = true;

        if (other.CompareTag("Water"))
            waterHit = true;

        // Activar engranajes con Fire + Wind
        if (fireHit && windHit && !puzzleActivated)
        {
            puzzleActivated = true;
            ActivateGears();
        }

        // Hacer caer los engranajes con Fire + Water
        if (fireHit && waterHit)
        {
            MakeGearsFall();
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

    void MakeGearsFall()
    {
        GameObject[] gears = GameObject.FindGameObjectsWithTag("Engranaje");
        foreach (GameObject gear in gears)
        {
            GearBehavior gb = gear.GetComponent<GearBehavior>();
            if (gb != null)
                gb.MakeFall();
        }
    }
}
