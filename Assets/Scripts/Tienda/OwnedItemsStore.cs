using System.Collections.Generic;
using UnityEngine;

public static class OwnedItemsStore
{
    private const string PP_KEY = "store.owned.items";

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
}
