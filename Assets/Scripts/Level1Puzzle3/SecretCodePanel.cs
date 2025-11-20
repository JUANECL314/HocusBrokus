using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SecretCodePanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public Transform iconsContainer; // needs HorizontalLayoutGroup
    public Sprite[] codeSprites;      // PNGs you want to display

    [Header("Settings")]
    public float iconSize = 100f;     // width & height of the icons
    public float spacing = 10f;       // horizontal spacing between PNGs

    private void Start()
    {
        RefreshPanel();
    }

    public void RefreshPanel()
    {
        // Clear previous children
        foreach (Transform child in iconsContainer)
            Destroy(child.gameObject);

        // Set spacing in layout group
        HorizontalLayoutGroup layout = iconsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
            layout.spacing = spacing;

        // Instantiate icons
        foreach (Sprite s in codeSprites)
        {
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(iconsContainer, false);

            Image img = iconObj.GetComponent<Image>();
            img.sprite = s;
            img.preserveAspect = true;

            RectTransform rt = iconObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(iconSize, iconSize);
        }
    }
}


