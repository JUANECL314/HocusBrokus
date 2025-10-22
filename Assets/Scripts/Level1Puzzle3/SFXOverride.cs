using UnityEngine;

[DisallowMultipleComponent]
public class SfxAreaOverride : MonoBehaviour
{
    [Header("Override 3D (por objeto)")]
    public bool enabledOverride = true;

    [Tooltip("Mezcla 3D. 1 = 100% 3D")]
    [Range(0f, 1f)] public float spatialBlend = 1f;

    public AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;

    [Tooltip("Distancia a volumen completo")]
    public float minDistance = 2f;

    [Tooltip("Distancia máxima audible")]
    public float maxDistance = 50f;
}
