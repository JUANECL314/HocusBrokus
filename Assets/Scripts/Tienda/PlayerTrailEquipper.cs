using System;
using UnityEngine;

/// <summary>
/// Activa/desactiva la trail del jugador según la trail equipada en la tienda.
/// Usa OwnedItemsStore.GetEquippedTrailSku().
/// 
/// Versión simple: soporta UNA trail con un SKU específico.
/// Si tienes varias trails distintas, puedes duplicar este script
/// en diferentes TrailRenderer con diferentes requiredSku.
/// </summary>
public class PlayerTrailEquipper : MonoBehaviour
{
    [Header("Trail")]
    public TrailRenderer trailRenderer;       // arrástrale el TrailSpawn (o el TrailRenderer)
    [Tooltip("SKU de la trail que debe estar equipada para activar este TrailRenderer")]
    public string requiredSku = "trail_basic"; // <- pon aquí el SKU de tu ítem de tienda

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
        RefreshTrail();
    }

    public void RefreshTrail()
    {
        if (!trailRenderer) return;

        string equipped = OwnedItemsStore.GetEquippedTrailSku();
        bool active = !string.IsNullOrEmpty(equipped) &&
                      string.Equals(equipped, requiredSku, StringComparison.OrdinalIgnoreCase);

        trailRenderer.gameObject.SetActive(active);
    }
}
