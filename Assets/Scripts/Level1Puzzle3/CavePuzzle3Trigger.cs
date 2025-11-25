using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

public class CavePuzzle3TriggerFade : MonoBehaviour
{
    [Header("Assign your Text UI object")]
    public Graphic textGraphic;  // works for TMP or regular Text

    [Header("Fade Settings")]
    public float fadeDuration = 1.5f;

    private Coroutine fadeRoutine;

    private void Start()
    {
        if (textGraphic != null)
        {
            Color c = textGraphic.color;
            c.a = 0f;
            textGraphic.color = c;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeText(1f));  // fade in
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeText(0f));  // fade out
        }
    }

    private System.Collections.IEnumerator FadeText(float targetAlpha)
    {
        if (textGraphic == null)
            yield break;

        float startAlpha = textGraphic.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            Color c = textGraphic.color;
            c.a = newAlpha;
            textGraphic.color = c;

            Debug.Log($"[Puzzle3Fade] Alpha: {newAlpha}");

            yield return null;
        }

        // Ensure perfect final alpha
        Color final = textGraphic.color;
        final.a = targetAlpha;
        textGraphic.color = final;

        Debug.Log($"[Puzzle3Fade] Final Alpha: {targetAlpha}");
    }
}
