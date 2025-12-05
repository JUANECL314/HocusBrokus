using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
public class DoorProgressUI : MonoBehaviourPun
{
    /*public ElementalPuzzle puzzle;
    public Slider slider;
    public TMP_Text statusText;     // opcional

    void Start()
    {
        if(slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 0f;
            slider.interactable = false;
        }
        if(statusText != null)
        {
            statusText.text = "0%";
        }
    }
    void Reset()
    {
        slider = GetComponentInChildren<Slider>();
        statusText = GetComponentInChildren<Text>();
    }

    void Update()
    {
        if (puzzle == null || slider == null) return;

        slider.gameObject.SetActive(true);
        if (statusText != null) statusText.gameObject.SetActive(true);

        slider.normalizedValue = puzzle.Progress01;
        if (statusText != null)
            statusText.text = puzzle.IsPaused ? "PAUSADO" :
                (puzzle.IsActivated ? $"{Mathf.RoundToInt(puzzle.Progress01 * 100f)}%" : "0%");
    }*/
}
