using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GearManager : MonoBehaviour
{
    public static GearManager Instance;

    public List<GearElemental> gears = new List<GearElemental>();
    public bool isActivated = false;
    private bool isOverheated = false;
    private bool isCooling = false;

    private void Awake()
    {
        Instance = this;
    }

    public void ActivateGears()
    {
        if (isActivated) return;
        isActivated = true;
        foreach (var gear in gears)
            gear.StartRotation();
    }

    public void CoolDownGears()
    {
        if (!isActivated) return;
        isCooling = true;
        foreach (var gear in gears)
            gear.CoolDown();
        StopAllCoroutines();
        StartCoroutine(ResetOverheat());
    }

    IEnumerator ResetOverheat()
    {
        yield return new WaitForSeconds(1f);
        isCooling = false;
    }

    public void StartOverheatCountdown()
    {
        if (isOverheated || isCooling) return;
        StartCoroutine(OverheatRoutine());
    }

    IEnumerator OverheatRoutine()
    {
        isOverheated = true;
        yield return new WaitForSeconds(5f);
        foreach (var gear in gears)
            gear.StopRotation();
        isActivated = false;
        isOverheated = false;
    }

    public void ResetAllGears()
    {
        StopAllCoroutines();
        isActivated = false;
        isOverheated = false;
        foreach (var gear in gears)
            gear.ResetGear();
    }
}
