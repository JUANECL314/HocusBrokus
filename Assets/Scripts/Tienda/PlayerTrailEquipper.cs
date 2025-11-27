using System;
using UnityEngine;

public class PlayerTrailEquipper : MonoBehaviour
{
    [Header("Trail")]
    [Tooltip("TrailRenderer que se debe encender/apagar")]
    public TrailRenderer trailRenderer;   // arrastra aquí el TrailRenderer

    [Tooltip("SKU que debe estar equipada para activar este trail")]
    public string requiredSku = "trail_basic";

    [Header("Debug")]
    public bool debugLogs = true;

    [Tooltip("Ignora la tienda y deja el trail siempre encendido (para pruebas).")]
    public bool forceAlwaysOnTest = false;

    float defaultTime = 0.5f;

    void Awake()
    {
        if (!trailRenderer)
            trailRenderer = GetComponent<TrailRenderer>();

        if (trailRenderer)
            defaultTime = Mathf.Max(0.3f, trailRenderer.time);
    }

    void Start()
    {
        RefreshTrail();
    }

    void OnEnable()
    {
        RefreshTrail();
    }

    public void RefreshTrail()
    {
        if (!trailRenderer)
        {
            if (debugLogs)
                Debug.LogWarning("[PlayerTrailEquipper] No TrailRenderer asignado.", this);
            return;
        }

        bool active;

        if (forceAlwaysOnTest)
        {
            active = true;
            if (debugLogs)
                Debug.Log("[PlayerTrailEquipper] forceAlwaysOnTest = true → activando trail sin mirar tienda.", this);
        }
        else
        {
            string equipped = OwnedItemsStore.GetEquippedTrailSku();
            active = !string.IsNullOrEmpty(equipped) &&
                     string.Equals(equipped, requiredSku, StringComparison.OrdinalIgnoreCase);

            if (debugLogs)
                Debug.Log($"[PlayerTrailEquipper] requiredSku='{requiredSku}', equipped='{equipped}', active={active}", this);
        }

        // Aseguramos TODO:
        trailRenderer.enabled = active;
        trailRenderer.emitting = active;
        trailRenderer.time = active ? defaultTime : 0f;

        if (!active)
            trailRenderer.Clear();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // si arrastras el TrailRenderer en el inspector, recalcula defaultTime
        if (trailRenderer)
            defaultTime = Mathf.Max(0.3f, trailRenderer.time);
    }
#endif
}
