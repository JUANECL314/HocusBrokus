using System.Collections.Generic;
using UnityEngine;

public class LaserTarget : MonoBehaviour
{
    [Tooltip("How many distinct beams must be simultaneously hitting to trigger the success message.")]
    public int requiredBeams = 2;

    [Tooltip("How many seconds the required number of beams must continuously hit to trigger.")]
    public float requiredDuration = 5f;

    private HashSet<int> activeBeams = new HashSet<int>();
    private float timer = 0f;
    private bool triggered = false;

    public void Activate(int beamId)
    {
        if (activeBeams.Add(beamId))
        {
            Debug.Log($"LaserTarget: beam {beamId} started hitting. Count = {activeBeams.Count}");
        }
    }

    public void Deactivate(int beamId)
    {
        if (activeBeams.Remove(beamId))
        {
            Debug.Log($"LaserTarget: beam {beamId} stopped hitting. Count = {activeBeams.Count}");
        }
    }

    void Update()
    {
        if (activeBeams.Count >= requiredBeams)
        {
            timer += Time.deltaTime;
            if (timer >= requiredDuration && !triggered)
            {
                triggered = true;
                OnRequiredBeamsHeld();
            }
        }
        else
        {
            timer = 0f;
            triggered = false;
        }
    }

    private void OnRequiredBeamsHeld()
    {
        Debug.Log($"LaserTarget: required {requiredBeams} beams held for {requiredDuration} seconds — SUCCESS!");
        // Add effects, sounds, etc.
    }
}
