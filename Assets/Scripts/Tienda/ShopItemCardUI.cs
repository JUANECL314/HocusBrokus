using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI txtTitle;
    [SerializeField] private TextMeshProUGUI txtPrice;
    [SerializeField] private Button btnComprar;
    [SerializeField] private GameObject badgeComprado; // opcional (un objeto con texto "Comprado")
    [SerializeField] private CanvasGroup canvasGroup;  // opcional, para deshabilitar visualmente

    // Datos
    public string Sku { get; private set; }
    private Action _onBuy;

    public void Bind(string sku, string title, int price, bool owned, Action onBuy)
    {
        Sku = sku;
        _onBuy = onBuy;

        if (txtTitle) txtTitle.text = title;
        if (txtPrice) txtPrice.text = $"$ {price}";
        if (btnComprar)
        {
            btnComprar.onClick.RemoveAllListeners();
            btnComprar.onClick.AddListener(() => _onBuy?.Invoke());
        }

        SetOwned(owned);
    }

    public void SetOwned(bool owned)
    {
        if (btnComprar) btnComprar.interactable = !owned;
        if (badgeComprado) badgeComprado.SetActive(owned);

        // opcional: atenuar tarjeta si ya es del usuario
        if (canvasGroup)
        {
            canvasGroup.alpha = owned ? 0.6f : 1f;
            canvasGroup.interactable = !owned;
            canvasGroup.blocksRaycasts = !owned;
        }

        // Si no tienes badge, puedes cambiar el texto del botón:
        if (!badgeComprado && btnComprar)
        {
            var t = btnComprar.GetComponentInChildren<TextMeshProUGUI>();
            if (t) t.text = owned ? "Comprado" : "Comprar";
        }
    }
}
