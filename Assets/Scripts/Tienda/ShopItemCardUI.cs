using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ShopItemCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtPrice;
    [SerializeField] private Button btnComprar;
    [SerializeField] private GameObject badgeComprado;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image bgImage;          // icono/fondo
    [Header("Equipar")]
    [SerializeField] private Button btnEquip;        // NUEVO botón Equipar
    [SerializeField] private GameObject badgeEquipped; // opcional “Equipado”

    // cache local de sprites por URL (por proceso)
    private static readonly Dictionary<string, Sprite> _spriteCache = new(StringComparer.OrdinalIgnoreCase);

    public string Sku { get; private set; }

    private Action _onBuy;
    private Action _onEquip;

    public bool IsOwned { get; private set; }
    public bool IsEquipped { get; private set; }

    /// <param name="imageUrl">URL pública de imagen (opcional)</param>
    public void Bind(
        string sku,
        string title,
        int price,
        bool isOwned,
        bool isEquipped,
        string imageUrl,
        Action onBuy,
        Action onEquip)
    {
        Sku = sku;
        _onBuy = onBuy;
        _onEquip = onEquip;

        if (txtTitle) txtTitle.text = string.IsNullOrEmpty(title) ? sku : title;
        if (txtPrice) txtPrice.text = $"$ {price}";

        // Botón comprar
        if (btnComprar)
        {
            btnComprar.onClick.RemoveAllListeners();
            btnComprar.onClick.AddListener(() => _onBuy?.Invoke());
        }

        // Botón equipar
        if (btnEquip)
        {
            btnEquip.onClick.RemoveAllListeners();
            btnEquip.onClick.AddListener(() => _onEquip?.Invoke());
        }

        SetOwned(isOwned);
        SetEquipped(isEquipped);

        // Cargar imagen
        if (bgImage)
        {
            bgImage.raycastTarget = false; // para que no bloquee clicks
            if (!string.IsNullOrEmpty(imageUrl))
                StartCoroutine(CoLoadSprite(imageUrl));
            else
                bgImage.sprite = null;
        }
    }

    public void SetOwned(bool owned)
    {
        IsOwned = owned;

        if (btnComprar) btnComprar.interactable = !owned;
        if (badgeComprado) badgeComprado.SetActive(owned);

        // IMPORTANTE: ya no bloqueamos el CanvasGroup cuando es owned,
        // para poder seguir haciendo clic en "Equipar".
        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // Si no tienes badgeComprado, actualiza el texto del botón comprar
        if (!badgeComprado && btnComprar)
        {
            var t = btnComprar.GetComponentInChildren<TextMeshProUGUI>();
            if (t) t.text = owned ? "Comprado" : "Comprar";
        }

        // Si el ítem no es owned, asegúrate de que no aparezca como equipado.
        if (!owned)
        {
            SetEquipped(false);
        }
    }

    public void SetEquipped(bool equipped)
    {
        IsEquipped = equipped;

        if (btnEquip)
        {
            var t = btnEquip.GetComponentInChildren<TextMeshProUGUI>();
            if (t) t.text = equipped ? "Equipada" : "Equipar";
            btnEquip.interactable = IsOwned; // solo si lo tienes
        }

        if (badgeEquipped)
            badgeEquipped.SetActive(equipped);
    }

    private IEnumerator CoLoadSprite(string url)
    {
        if (_spriteCache.TryGetValue(url, out var cached))
        {
            bgImage.sprite = cached;
            bgImage.preserveAspect = true;
            yield break;
        }

        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
        {
            Debug.LogWarning($"[ShopCard] No se pudo bajar imagen {url}: {req.error}");
            yield break;
        }

        var tex = DownloadHandlerTexture.GetContent(req);
        if (tex == null) yield break;

        var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100);
        _spriteCache[url] = spr;

        if (bgImage)
        {
            bgImage.sprite = spr;
            bgImage.preserveAspect = true;
        }
    }
}
