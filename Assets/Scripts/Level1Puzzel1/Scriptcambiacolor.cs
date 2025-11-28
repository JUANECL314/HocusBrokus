using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Scriptcambiacolor : MonoBehaviour
{
    public Slider slider;
    public Image fill;
    public Image background;
    public TextMeshProUGUI percentageText;

    public float smoothSpeed = 5f;

    private float targetValue = 0f;

    private void Start()
    {
        slider.value = 0f;

        UpdateText();
    }

    private void Update()
    {
        slider.value = Mathf.Lerp(slider.value, targetValue, Time.deltaTime * smoothSpeed);

        Color startColor = new Color(0.2f, 0.4f, 1f);   // Azul
        Color endColor = new Color(1f, 0.6f, 0.1f);     // Naranja cálido

        Color targetColor = Color.Lerp(startColor, endColor, slider.value);

        fill.color = Color.Lerp(fill.color, targetColor, Time.deltaTime * 6f);
        background.color = Color.Lerp(background.color, targetColor, Time.deltaTime * 6f);

        UpdateText();
    }

    private void UpdateText()
    {
        percentageText.text = Mathf.RoundToInt(slider.value * 100f) + "%";
    }

    public void UpdateProgress(float p)
    {
        targetValue = Mathf.Clamp01(p);
    }
}
