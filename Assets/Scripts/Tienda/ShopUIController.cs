using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class ShopUIController : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string apiBaseUrl = "https://hokusbackend-production.up.railway.app";
    [SerializeField] private float requestTimeoutSeconds = 15f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI txtCoins;
    [SerializeField] private Transform contentGrid;       // Grid Layout Group
    [SerializeField] private GameObject itemCardPrefab;   // Prefab con ShopItemCardUI
    [SerializeField] private TextMeshProUGUI txtStatus;   // Mensajes cortos

    [Header("Opcional")]
    [SerializeField] private bool refreshOnEnable = true;

    // --- DTOs que mapean tu backend actual ---
    [Serializable] private class WalletResponse { public int user_id; public int balance; public string updated_at; }

    [Serializable]
    private class ShopItem
    {
        public int id;
        public string sku;
        public string title;
        public string description;
        public int price;
        public string image_url;
        public bool active;
    }

    [Serializable]
    private class InventoryItem
    {
        public string sku;
        public string title;
        public string description;
        public int price;
        public string image_url;
        public int quantity;
        public string purchased_at;
    }
    [Serializable] private class CatalogResponse { public List<ShopItem> items; }

    [Serializable] private class PurchaseRequest { public int user_id; public string sku; public int quantity = 1; }

    // --- Estado local ---
    private readonly HashSet<string> _ownedSkus = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ShopItemCardUI> _cardsBySku = new(StringComparer.OrdinalIgnoreCase);

    // NUEVO: cuál trail está equipada (SKU). La leemos/guardamos en PlayerPrefs usando OwnedItemsStore.
    private string _equippedTrailSku = "";

    private void OnEnable()
    {
        if (refreshOnEnable) StartCoroutine(LoadStore());
    }

    public void Refresh() => StartCoroutine(LoadStore());

    private IEnumerator LoadStore()
    {
        if (!EnsureLogged()) yield break;

        SetStatus("Cargando tienda.");
        _ownedSkus.Clear();
        _cardsBySku.Clear();

        // Leer trail equipada localmente
        _equippedTrailSku = OwnedItemsStore.GetEquippedTrailSku();

        yield return StartCoroutine(CoLoadWallet());
        yield return StartCoroutine(CoLoadInventory());
        yield return StartCoroutine(CoLoadCatalog());

        SetStatus("");
    }

    private bool EnsureLogged()
    {
        if (string.IsNullOrEmpty(AuthState.Token) || AuthState.UserId <= 0)
        {
            SetStatus("Inicia sesión para ver la tienda.");
            return false;
        }
        return true;
    }

    // ---------------------- Cargar datos ----------------------

    private IEnumerator CoLoadWallet()
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/wallet/{AuthState.UserId}";
        using var req = UnityWebRequest.Get(url);
        req.timeout = Mathf.RoundToInt(requestTimeoutSeconds);
        SetAuth(req);

        yield return req.SendWebRequest();

        if (HasError(req))
        {
            SetStatus("No se pudo cargar tu saldo.");
            Debug.LogWarning($"[Shop] Wallet error {req.responseCode}: {req.downloadHandler.text}");
            yield break;
        }

        var data = JsonUtility.FromJson<WalletResponse>(req.downloadHandler.text);
        if (txtCoins) txtCoins.text = data != null ? $"Monedas: {data.balance}" : "Monedas: -";
    }

    private IEnumerator CoLoadInventory()
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/shop/inventory?userId={AuthState.UserId}";
        using var req = UnityWebRequest.Get(url);
        req.timeout = Mathf.RoundToInt(requestTimeoutSeconds);
        SetAuth(req);

        yield return req.SendWebRequest();

        if (HasError(req))
        {
            Debug.LogWarning($"[Shop] Inventario no disponible: {req.responseCode} {req.downloadHandler.text}");
            yield break;
        }

        var invArray = JsonHelper.FromJsonArray<InventoryItem>(req.downloadHandler.text);
        _ownedSkus.Clear();
        foreach (var it in invArray)
        {
            if (it != null && !string.IsNullOrEmpty(it.sku) && it.quantity > 0)
                _ownedSkus.Add(it.sku);
        }
    }

    private IEnumerator CoLoadCatalog()
    {
        if (contentGrid == null)
        {
            SetStatus("Falta Content Grid en el inspector.");
            yield break;
        }

        // Limpia el grid
        for (int i = contentGrid.childCount - 1; i >= 0; i--)
            Destroy(contentGrid.GetChild(i).gameObject);
        _cardsBySku.Clear();

        var baseUrl = apiBaseUrl != null ? apiBaseUrl.TrimEnd('/') : "";
        var url = $"{baseUrl}/shop/catalog";

        using var req = UnityWebRequest.Get(url);
        req.timeout = Mathf.RoundToInt(requestTimeoutSeconds);
        SetAuth(req);

        Debug.Log($"[Shop] Solicitando catálogo: {url}");

        yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        bool transportError = req.result != UnityWebRequest.Result.Success;
#else
        bool transportError = req.isNetworkError || req.isHttpError;
#endif

        if (transportError || req.responseCode < 200 || req.responseCode >= 300)
        {
            SetStatus($"No se pudo cargar el catálogo. ({req.responseCode})");
            Debug.LogWarning($"[Shop] Catálogo error {req.responseCode}: {req.downloadHandler.text}\nURL: {url}");
            yield break;
        }

        var body = req.downloadHandler.text;
        Debug.Log($"[Shop] Catálogo recibido: {body}");

        // Soportar ambas formas: array puro o { items: [...] }
        List<ShopItem> items = null;

        // 1) ¿array puro?
        if (body.TrimStart().StartsWith("["))
        {
            try
            {
                var arr = JsonHelper.FromJsonArray<ShopItem>(body);
                items = new List<ShopItem>(arr);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Shop] No pude parsear array puro: {e}");
            }
        }
        else
        {
            // 2) ¿objeto con 'items'?
            try
            {
                var wrapper = JsonUtility.FromJson<CatalogResponse>(body);
                if (wrapper != null && wrapper.items != null)
                    items = wrapper.items;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Shop] No pude parsear objeto con items: {e}");
            }
        }

        if (items == null || items.Count == 0)
        {
            SetStatus("Catálogo vacío.");
            Debug.LogWarning($"[Shop] Catálogo vacío o no parseable. Body: {body}");
            yield break;
        }

        if (itemCardPrefab == null)
        {
            SetStatus("Falta asignar Item Card Prefab en el inspector.");
            yield break;
        }

        foreach (var it in items)
        {
            if (it == null || string.IsNullOrEmpty(it.sku)) continue;

            var go = Instantiate(itemCardPrefab, contentGrid);
            var ui = go.GetComponent<ShopItemCardUI>();
            if (ui == null)
            {
                Debug.LogError("[Shop] El prefab no tiene ShopItemCardUI.");
                Destroy(go);
                continue;
            }

            bool isOwned = _ownedSkus.Contains(it.sku);
            bool isEquipped = string.Equals(it.sku, _equippedTrailSku, StringComparison.OrdinalIgnoreCase);

            ui.Bind(
                sku: it.sku,
                title: string.IsNullOrEmpty(it.title) ? it.sku : it.title,
                price: it.price,
                isOwned: isOwned,
                isEquipped: isEquipped,
                imageUrl: it.image_url,
                onBuy: () =>
                {
                    if (!isOwned) StartCoroutine(CoPurchase(it.sku, 1));
                },
                onEquip: () =>
                {
                    OnEquipTrail(it.sku);
                }
            );

            _cardsBySku[it.sku] = ui;
        }

        SetStatus("");
    }

    // ---------------------- Comprar ----------------------

    private IEnumerator CoPurchase(string sku, int qty)
    {
        if (!_cardsBySku.TryGetValue(sku, out var card))
            yield break;

        if (_ownedSkus.Contains(sku))
        {
            SetStatus("Ya lo tienes.");
            card.SetOwned(true);
            yield break;
        }

        SetStatus("Procesando compra.");

        var payload = new PurchaseRequest { user_id = AuthState.UserId, sku = sku, quantity = qty };
        var json = JsonUtility.ToJson(payload);

        var url = $"{apiBaseUrl.TrimEnd('/')}/shop/purchase";
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = Mathf.RoundToInt(requestTimeoutSeconds);
        req.SetRequestHeader("Content-Type", "application/json");
        SetAuth(req);

        yield return req.SendWebRequest();

        if (HasError(req))
        {
            SetStatus(MapFriendlyError((int)req.responseCode, req.downloadHandler.text));
            yield break;
        }

        // Compra OK: marcar y refrescar saldo
        _ownedSkus.Add(sku);
        card.SetOwned(true);

        yield return StartCoroutine(CoLoadWallet()); // refresca Monedas: X
        SetStatus("Compra realizada.");
    }

    // ---------------------- EQUIPAR TRAIL ----------------------

    /// <summary>
    /// Lógica de equipar/desequipar una trail usando el mismo SKU del catálogo.
    /// </summary>
    private void OnEquipTrail(string sku)
    {
        if (!_ownedSkus.Contains(sku))
        {
            SetStatus("Primero compra la trail para equiparla.");
            return;
        }

        // Toggle: si ya está equipada esta misma, la quitamos.
        if (string.Equals(_equippedTrailSku, sku, StringComparison.OrdinalIgnoreCase))
        {
            _equippedTrailSku = "";
            OwnedItemsStore.SetEquippedTrailSku("");
            SetStatus("Trail quitada.");
        }
        else
        {
            _equippedTrailSku = sku;
            OwnedItemsStore.SetEquippedTrailSku(sku);
            SetStatus("Trail equipada.");
        }

        // Actualizar todas las cards
        foreach (var kvp in _cardsBySku)
        {
            bool eq = string.Equals(kvp.Key, _equippedTrailSku, StringComparison.OrdinalIgnoreCase);
            kvp.Value.SetEquipped(eq);
        }
    }

    // ---------------------- Helpers ----------------------

    private void SetAuth(UnityWebRequest req)
    {
        if (!string.IsNullOrEmpty(AuthState.Token))
            req.SetRequestHeader("Authorization", "Bearer " + AuthState.Token);
    }

    private bool HasError(UnityWebRequest req)
    {
#if UNITY_2020_2_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success) return true;
#else
        if (req.isNetworkError || req.isHttpError) return true;
#endif
        return req.responseCode < 200 || req.responseCode >= 300;
    }

    private void SetStatus(string msg)
    {
        if (txtStatus) txtStatus.text = msg;
        Debug.Log("[Shop] " + msg);
    }

    private string MapFriendlyError(int status, string body)
    {
        var low = (body ?? "").ToLowerInvariant();
        if (status == 401 || status == 403) return "Sesión no válida. Inicia sesión.";
        if (status == 409 || low.Contains("fondos")) return "No tienes suficientes monedas.";
        if (status == 404) return "El artículo no está disponible.";
        if (status >= 500) return "Error del servidor. Intenta más tarde.";
        return "No se pudo completar la compra.";
    }

    // JsonUtility no soporta arrays top-level; helper para envolver y parsear arrays.
    public static class JsonHelper
    {
        [Serializable] private class Wrapper<T> { public T[] items; }
        public static T[] FromJsonArray<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<T>();
            string wrapped = "{\"items\":" + json + "}"; // envolvemos el array en un objeto
            var w = JsonUtility.FromJson<Wrapper<T>>(wrapped);
            return w?.items ?? Array.Empty<T>();
        }
    }
}
