#if PHOTON_UNITY_NETWORKING
using Photon.Realtime;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PlayerCardUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Toggle tglLiderazgo;
    [SerializeField] private Toggle tglCalma;
    [SerializeField] private Toggle tglCoordinacion;
    [SerializeField] private Toggle tglApoyo;
    [SerializeField] private Toggle tglCreatividad;

    // Respaldo común
    private string _receiverId;
    private string _receiverName;

#if PHOTON_UNITY_NETWORKING
    private Player _player; // solo cuando hay Photon
#endif

    private void Awake()
    {
        // Intento auto-descubrir refs si faltan (útil cuando se rehace el prefab)
        AutoWireIfMissing();
        WarnIfMissingRefs();
    }

    private void AutoWireIfMissing()
    {
        if (!playerNameText) playerNameText = GetComponentInChildren<TextMeshProUGUI>(true);

        // Si usas nombres estándar en el prefab, puedes ayudar a enlazar rápido:
        if (!tglLiderazgo) tglLiderazgo = FindToggleByNameContains("Lider", "Liderazgo");
        if (!tglCalma) tglCalma = FindToggleByNameContains("Calma");
        if (!tglCoordinacion) tglCoordinacion = FindToggleByNameContains("Coor", "Coordin");
        if (!tglApoyo) tglApoyo = FindToggleByNameContains("Apoyo");
        if (!tglCreatividad) tglCreatividad = FindToggleByNameContains("Creat", "Creatividad");
    }

    private Toggle FindToggleByNameContains(params string[] parts)
    {
        var toggles = GetComponentsInChildren<Toggle>(true);
        foreach (var t in toggles)
        {
            var n = t.gameObject.name.ToLowerInvariant();
            bool all = true;
            foreach (var p in parts)
            {
                if (!n.Contains(p.ToLowerInvariant())) { all = false; break; }
            }
            if (all) return t;
        }
        return null;
    }

    private void WarnIfMissingRefs()
    {
#if UNITY_EDITOR
        if (!playerNameText) Debug.LogWarning($"[PlayerCardUI] Falta 'playerNameText' en {name}");
        if (!tglLiderazgo) Debug.LogWarning($"[PlayerCardUI] Falta 'tglLiderazgo' en {name}");
        if (!tglCalma) Debug.LogWarning($"[PlayerCardUI] Falta 'tglCalma' en {name}");
        if (!tglCoordinacion) Debug.LogWarning($"[PlayerCardUI] Falta 'tglCoordinacion' en {name}");
        if (!tglApoyo) Debug.LogWarning($"[PlayerCardUI] Falta 'tglApoyo' en {name}");
        if (!tglCreatividad) Debug.LogWarning($"[PlayerCardUI] Falta 'tglCreatividad' en {name}");
#endif
    }

    // --- Bind para Photon ---
#if PHOTON_UNITY_NETWORKING
    public void Bind(Player photonPlayer)
    {
        _player = photonPlayer;
        _receiverId = photonPlayer?.UserId ?? photonPlayer?.ActorNumber.ToString();
        _receiverName = photonPlayer?.NickName ?? $"Player {photonPlayer?.ActorNumber}";

        if (playerNameText) playerNameText.text = _receiverName;

        Debug.Log($"[PlayerCardUI] Bind Photon -> '{_receiverName}' (UserId: {_receiverId})");
        ClearSelection();
    }
#endif

    // --- Bind para Mock/Offline (string,string) ---
    public void Bind(string id, string displayName)
    {
        _receiverId = id;
        _receiverName = displayName;

        if (playerNameText) playerNameText.text = _receiverName;

        Debug.Log($"[PlayerCardUI] Bind Mock -> '{displayName}' (Id: {id})");
        ClearSelection();
    }

    public string ReceiverUserId => _receiverId;
    public string ReceiverDisplayName => _receiverName;

    public bool HasSelection(out EndorsementType type)
    {
        if (tglLiderazgo && tglLiderazgo.isOn) { type = EndorsementType.Liderazgo; return true; }
        if (tglCalma && tglCalma.isOn) { type = EndorsementType.Calma; return true; }
        if (tglCoordinacion && tglCoordinacion.isOn) { type = EndorsementType.Coordinacion; return true; }
        if (tglApoyo && tglApoyo.isOn) { type = EndorsementType.Apoyo; return true; }
        if (tglCreatividad && tglCreatividad.isOn) { type = EndorsementType.Creatividad; return true; }
        type = default;
        return false;
    }

    public void ClearSelection()
    {
        if (tglLiderazgo) tglLiderazgo.isOn = false;
        if (tglCalma) tglCalma.isOn = false;
        if (tglCoordinacion) tglCoordinacion.isOn = false;
        if (tglApoyo) tglApoyo.isOn = false;
        if (tglCreatividad) tglCreatividad.isOn = false;
    }
}
