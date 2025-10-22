using UnityEngine;

[ExecuteAlways]
public class SoundManagerGizmos : MonoBehaviour
{
    public bool enabledGizmos = true;
    public Color minColor = new Color(0f, 1f, 0f, 0.25f);
    public Color maxColor = new Color(1f, 0f, 0f, 0.18f);
    public bool drawToListener = false;
    public Color lineColor = new Color(1f, 1f, 0f, 0.6f);

    void OnDrawGizmos()
    {
        if (!enabledGizmos) return;

        var sources = GetComponentsInChildren<AudioSource>(true);
        if (sources == null) return;

        AudioListener listener = null;
        if (drawToListener) listener = FindObjectOfType<AudioListener>();

        foreach (var src in sources)
        {
            if (src == null) continue;

            // MIN
            Gizmos.color = minColor;
            Gizmos.DrawWireSphere(src.transform.position, src.minDistance);

            // MAX
            Gizmos.color = maxColor;
            Gizmos.DrawWireSphere(src.transform.position, src.maxDistance);

            // Línea al listener (opcional)
            if (drawToListener && listener != null && listener.isActiveAndEnabled)
            {
                Gizmos.color = lineColor;
                Gizmos.DrawLine(src.transform.position, listener.transform.position);
            }
        }
    }
}
