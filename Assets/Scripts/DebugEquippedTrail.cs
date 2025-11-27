using UnityEngine;

public class DebugEquippedTrail : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[DebugEquippedTrail] EquippedTrailSku = " + OwnedItemsStore.GetEquippedTrailSku());
    }
}
