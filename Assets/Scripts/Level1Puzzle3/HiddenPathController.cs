using UnityEngine;
using System.Collections;
using Photon.Pun;

public class HiddenPathController : MonoBehaviourPun
{
    [Header("Movement")]
    public float targetHeight = 170f;
    public float speed = 5f;

    [Header("Audio Fade Settings")]
    public float fadeInTime = 0.8f;
    public float fadeOutTime = 1.0f;

    private bool activated = false;
    private AudioSource risingAudioSource;

    // Call this to activate the path across all clients
    public void ShowPath()
    {
        if (!activated)
        {
            activated = true;
            photonView.RPC(nameof(RPC_ShowPath), RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_ShowPath()
    {
        StartCoroutine(PlayRisingSound());
        StartCoroutine(MoveUp());
    }

    private IEnumerator MoveUp()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(startPos.x, targetHeight, startPos.z);

        float distance = Vector3.Distance(startPos, endPos);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * speed / distance;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
    }

    private IEnumerator PlayRisingSound()
    {
        // Get an AudioSource with all SoundManager settings applied
        risingAudioSource = SoundManager.Instance.PlayAndGetSource(
            SfxKey.RisingHiddenPath,
            transform // 3D position follows the rising object
        );

        if (risingAudioSource == null)
            yield break;

        float originalVolume = risingAudioSource.volume;

        // --- Fade In ---
        risingAudioSource.volume = 0f;
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            risingAudioSource.volume = Mathf.Lerp(0f, originalVolume, t / fadeInTime);
            yield return null;
        }
        risingAudioSource.volume = originalVolume;

        // --- Wait until movement stops ---
        Vector3 lastPos = transform.position;
        while (Vector3.Distance(lastPos, transform.position) > 0.001f)
        {
            lastPos = transform.position;
            yield return null;
        }

        // --- Fade Out ---
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            risingAudioSource.volume = Mathf.Lerp(originalVolume, 0f, t / fadeOutTime);
            yield return null;
        }

        risingAudioSource.Stop();
        risingAudioSource.volume = originalVolume;
    }
}
