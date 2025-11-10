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
    [SerializeField] private string apiBaseUrl = "http://127.0.0.1:8000";
    [SerializeField] private float requestTimeoutSeconds = 15f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI txtCoins;
    [SerializeField] private Transform contentGrid;     // Grid Layout Group
    [SerializeField] private GameObject itemCardPrefab; // Prefab con ShopItemCardUI
    [SerializeField] private TextMeshProUGUI txtStatus; // Mensajes cortos

    [Header("Opcional")]
    [SerializeField] private bool refreshOnEnable = true;

    // DTOs
    [Serializable] private class WalletResponse { public int user_id; public int balance; public string updated_at; }
    [Serializable] private class ShopItem { public string sku; public string title; public int price; public string image_url; }
    [Serializable] private class CatalogResponse { public List<ShopItem> items; }

    [Serializable] private class InventoryItem { public string sku; public string title; public int quantity; public string purchased_at; }
    [Serializable] private class InventoryResponse { public List<InventoryItem> items; }

    [Serializable] private class PurchaseRequest { public int user_id; public string sku; public int quantity = 1; }
    [Serializable] private class PurchaseResponse { public int balance; public InventoryItem item; }

    // Estado
    private readonly HashSet<string> _ownedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ShopItemCardUI> _cardsBySku = new Dictionary<string, ShopItemCardUI>(StringComparer.OrdinalIgnoreCase);

    private void OnEnable()
    {
        if (refreshOnEnable) StartCoroutine(LoadStore());
    }

    public void Refresh() => StartCoroutine(LoadStore());

    private IEnumerator LoadStore()
    {
        if (!EnsureLogged()) yield break;

        SetStatus("Cargando tienda...");
        _ownedSkus.Clear();
        _cardsBySku.Clear();

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

    private IEnumerator CoLoadWallet()
    {
        var url = $"{apiBaseUrl.TrimEnd('/')}/wallet?userId={AuthState.UserId}";
        using var req = UnityWebRequest.Get(url);
        req.timeout = Mathf.RoundToInt(requestTimeoutSeconds);
        SetAuth(req);

        yield return req.SendWebRequest();
        if (HasError(req))
        {
            SetStatus("No se pudo cargar tu saldo.");
            yield break;
        }

        var data = JsonUtility.FromJson<WalletResponse>(req.downloadHandler.text);
        if (txtCoins) txtCoins.text = $"Monedas: {data.balance}";
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
            // No es fatal; la tienda puede cargar sin inventario
            Debug.LogWarning($"[Shop] Inventario no disponible: {req.responseCode} {req.downloadHandler.text}");
            yield break;
        }

        var inv = JsonUtility.FromJson<InventoryResponse>(req.downloadHandler.text);
        _ownedSkus.Clear();
        if (inv != null && inv.items != null)
        {
            foreach (var it in inv.items)
            {
                if (it.quantity > 0 && !string.IsNullOrEmpty(it.sku))
                    _ownedSkus.Add(it.sku);
            }
        }
    }

    private IEnumerator CoLoadCatalog()
    {
        // Limpia grid
        foreach (Transform c in contentGrid) Destroy(c.gameObject);
        _cardsBySku.Clear();

        var url = $"{apiBaseUrl.TrimEnd('/')}/shop/catalog";
        using var req = UnityWebRequest.Get(url);
        req.timeout = Mathf.RoundToInt(requestTimeoutSeconds);
        SetAuth(req);

        yield return req.SendWebRequest();
        if (HasError(req))
        {
            SetStatus("No se pudo cargar el catálogo.");
            yield break;
        }

        var catalog = JsonUtility.FromJson<CatalogResponse>(req.downloadHandler.text);
        if (catalog?.items == null || catalog.items.Count == 0)
        {
            SetStatus("No hay artículos disponibles.");
            yield break;
        }

        foreach (var it in catalog.items)
        {
            var go = Instantiate(itemCardPrefab, contentGrid);
            var ui = go.GetComponent<ShopItemCardUI>();
            bool owned = _ownedSkus.Contains(it.sku);
            ui.Bind(it.sku, it.title, it.price, owned, () =>
            {
                if (!owned) StartCoroutine(CoPurchase(it.sku, 1));
            });

            _cardsBySku[it.sku] = ui;
        }
    }

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

        SetStatus("Procesando compra...");

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

        var resp = JsonUtility.FromJson<PurchaseResponse>(req.downloadHandler.text);
        if (txtCoins) txtCoins.text = $"Monedas: {resp.balance}";

        // Marca como comprado en UI y estado local
        _ownedSkus.Add(sku);
        card.SetOwned(true);
        SetStatus($"Comprado: {resp.item.title}");
    }

    // Helpers ---------------------------------------------------------------

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

    private void SetStatus(string msg) { if (txtStatus) txtStatus.text = msg; Debug.Log("[Shop] " + msg); }

    private string MapFriendlyError(int status, string body)
    {
        var low = (body ?? "").ToLowerInvariant();
        if (status == 401 || status == 403) return "Sesión no válida. Inicia sesión.";
        if (status == 409 || low.Contains("fondos")) return "No tienes suficientes monedas.";
        if (status == 404) return "El artículo no está disponible.";
        if (status >= 500) return "Error del servidor. Intenta más tarde.";
        return "No se pudo completar la compra.";
    }
}
