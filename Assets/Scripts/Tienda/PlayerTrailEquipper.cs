using System;
using UnityEngine;

public class PlayerTrailEquipper : MonoBehaviour
{
    [Header("Trail")]
    public TrailRenderer trailRenderer;       // arrástrale el TrailSpawn (o el TrailRenderer)

    [Tooltip("SKU de la trail que debe estar equipada para activar este TrailRenderer")]
    public string requiredSku = "trail_basic";

    [Header("Debug")]
    public bool debugLogs = true;

    private void Awake()
    {
        if (!trailRenderer)
            trailRenderer = GetComponentInChildren<TrailRenderer>(true);
    }

    private void Start()
    {
        RefreshTrail();
    }

    private void OnEnable()
    {
        // Por si el prefab persiste entre escenas
        RefreshTrail();
    }

    public void RefreshTrail()
    {
        if (!trailRenderer)
        {
            if (debugLogs)
                Debug.LogWarning("[PlayerTrailEquipper] No hay TrailRenderer asignado.", this);
            return;
        }

        string equipped = OwnedItemsStore.GetEquippedTrailSku();
        bool active = !string.IsNullOrEmpty(equipped) &&
                      string.Equals(equipped, requiredSku, StringComparison.OrdinalIgnoreCase);

        if (debugLogs)
        {
            Debug.Log(
                $"[PlayerTrailEquipper] requiredSku='{requiredSku}', equipped='{equipped}', active={active}",
                this
            );
        }

        trailRenderer.gameObject.SetActive(active);
    }
}
