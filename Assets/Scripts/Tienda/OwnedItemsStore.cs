using System.Collections.Generic;
using UnityEngine;

public static class OwnedItemsStore
{
    private const string PP_KEY = "store.owned.items";

    // NUEVO: PlayerPrefs para la trail equipada
    private const string PP_TRAIL_EQUIPPED = "cosmetic.trail.equipped";

    // --------- ÍTEMS COMPRADOS (legacy/local) ----------

    // Carga como HashSet<string>
    public static HashSet<string> LoadOwned()
    {
        var csv = PlayerPrefs.GetString(PP_KEY, "");
        var set = new HashSet<string>();
        if (!string.IsNullOrEmpty(csv))
        {
            var parts = csv.Split(',');
            foreach (var p in parts)
            {
                var x = p.Trim();
                if (!string.IsNullOrEmpty(x)) set.Add(x);
            }
        }
        return set;
    }

    public static void SaveOwned(HashSet<string> owned)
    {
        var csv = string.Join(",", owned);
        PlayerPrefs.SetString(PP_KEY, csv);
        PlayerPrefs.Save();
    }

    public static bool IsOwned(string itemId)
    {
        return LoadOwned().Contains(itemId);
    }

    public static void Add(string itemId)
    {
        var set = LoadOwned();
        if (set.Add(itemId))
        {
            SaveOwned(set);
        }
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(PP_KEY);
    }

    // --------- TRAIL EQUIPADA (cosmético) ----------

    /// <summary>SKU de la trail equipada actualmente (o "" si ninguna).</summary>
    public static string GetEquippedTrailSku()
    {
        return PlayerPrefs.GetString(PP_TRAIL_EQUIPPED, "");
    }

    /// <summary>Guarda la trail equipada (puede ser "" para quitar).</summary>
    public static void SetEquippedTrailSku(string sku)
    {
        if (sku == null) sku = "";
        PlayerPrefs.SetString(PP_TRAIL_EQUIPPED, sku);
        PlayerPrefs.Save();
    }
}
