using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class DoorProgressUI : MonoBehaviourPun
{
    public ElementalPuzzle puzzle;
    public Slider slider;
    public Text statusText;     // opcional
    [PunRPC]
    void Reset()
    {
        slider = GetComponentInChildren<Slider>();
        statusText = GetComponentInChildren<Text>();
    }
    [PunRPC]
    void Update()
    {
        if (puzzle == null || slider == null) return;

        // Mostrar solo cuando el puzzle esta activo
        bool visible = puzzle.IsActivated;
        // Mostrar siempre, aunque no este activo
        slider.gameObject.SetActive(true);
        if (statusText != null) statusText.gameObject.SetActive(true);

        slider.normalizedValue = puzzle.Progress01;
        if (statusText != null)
            statusText.text = puzzle.IsPaused ? "PAUSADO" :
                (puzzle.IsActivated ? $"{Mathf.RoundToInt(puzzle.Progress01 * 0f)}%" : "0%");
    }
}
