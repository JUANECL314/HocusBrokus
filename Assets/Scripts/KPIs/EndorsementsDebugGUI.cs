using System;
using System.Collections.Generic;
using UnityEngine;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
#endif

public class EndorsementsDebugGUI : MonoBehaviour
{
    [Header("Config")]
    public int maxEndorsements = 3;
    public bool useMockPlayers = true;
    public string mockNamePrefix = "Compañero ";
    public int mockPlayersCount = 3;
    public bool show = true; // puedes ocultar/mostrar en runtime

    private string _localUserId;
    private string _matchId;
    private Vector2 _scroll;

    private readonly List<(string id, string name)> _targets = new();
    private readonly Dictionary<string, EndorsementType?> _selected = new(); // por jugador: selección (o null)

    private void Awake()
    {
#if PHOTON_UNITY_NETWORKING
        _localUserId = PhotonNetwork.LocalPlayer?.UserId
                       ?? PhotonNetwork.LocalPlayer?.ActorNumber.ToString();
        _matchId = $"match-{PhotonNetwork.CurrentRoom?.Name}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
#else
        _localUserId = SystemInfo.deviceUniqueIdentifier;
        _matchId = $"match-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
#endif

        BuildTargets();
    }

    private void BuildTargets()
    {
        _targets.Clear();
        _selected.Clear();

        if (useMockPlayers)
        {
            for (int i = 1; i <= mockPlayersCount; i++)
            {
                var id = $"mock_{i}";
                var name = $"{mockNamePrefix}{i}";
                _targets.Add((id, name));
                _selected[id] = null;
            }
            return;
        }

#if PHOTON_UNITY_NETWORKING
        foreach (var p in PhotonNetwork.PlayerList)
        {
            var id = p.UserId ?? p.ActorNumber.ToString();
            if (id == _localUserId) continue; // no me endoso a mí mismo
            _targets.Add((id, p.NickName));
            _selected[id] = null;
        }
#endif
    }

    private int CountSelected()
    {
        int c = 0;
        foreach (var kv in _selected)
            if (kv.Value.HasValue) c++;
        return c;
    }

    private GUIStyle _box, _label, _btn;
    private void EnsureStyles()
    {
        _box ??= new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.UpperLeft, padding = new RectOffset(12, 12, 12, 12) };
        _label ??= new GUIStyle(GUI.skin.label) { fontSize = 14 };
        _btn ??= new GUIStyle(GUI.skin.button) { fontSize = 14 };
    }

    private void OnGUI()
    {
        if (!show) return;
        EnsureStyles();

        const int W = 520;
        const int H = 420;
        var rect = new Rect(20, 20, W, H);
        GUILayout.BeginArea(rect, "Endorsements (debug)", _box);
        {
            GUILayout.Label($"Seleccionados {CountSelected()}/{maxEndorsements}", _label);
            GUILayout.Space(6);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            foreach (var t in _targets)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(t.name, _label);

                // radios manuales (exclusivo por jugador)
                GUILayout.BeginHorizontal();
                DrawRadio(t.id, EndorsementType.Liderazgo, "Liderazgo");
                DrawRadio(t.id, EndorsementType.Calma, "Calma");
                DrawRadio(t.id, EndorsementType.Coordinacion, "Coordinación");
                DrawRadio(t.id, EndorsementType.Apoyo, "Apoyo");
                DrawRadio(t.id, EndorsementType.Creatividad, "Creatividad");
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUI.enabled = CountSelected() <= maxEndorsements;
            if (GUILayout.Button("Enviar", _btn, GUILayout.Height(28)))
                Send();
            GUI.enabled = true;
            if (GUILayout.Button("Omitir", _btn, GUILayout.Height(28)))
                show = false;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndArea();
    }

    private void DrawRadio(string targetId, EndorsementType value, string label)
    {
        bool isOn = _selected[targetId].HasValue && _selected[targetId].Value == value;
        bool next = GUILayout.Toggle(isOn, label, GUILayout.Height(24));
        if (next != isOn)
        {
            // si encendí este, dejo este valor; si lo apagué, lo pongo null
            _selected[targetId] = next ? value : (EndorsementType?)null;

            // límite: si me pasé, deshago el último cambio
            if (CountSelected() > maxEndorsements)
                _selected[targetId] = isOn ? value : (EndorsementType?)null;
        }
    }

    private void Send()
    {
        var list = new List<EndorsementPayload>();
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (var kv in _selected)
        {
            if (!kv.Value.HasValue) continue;
            list.Add(new EndorsementPayload
            {
                matchId = _matchId,
                giverUserId = _localUserId,
                receiverUserId = kv.Key,
                type = kv.Value.Value,
                unixTime = now
            });
        }

        if (list.Count == 0) { show = false; return; }

        if (EndorsementUploader.Instance == null)
        {
            var go = new GameObject("EndorsementUploader");
            go.AddComponent<EndorsementUploader>();
        }
        EndorsementUploader.Instance.EnqueueAndSend(list);

        Debug.Log($"[EndorsementsDebugGUI] Enviados {list.Count} endorsements.");
        show = false; // cierra la ventana
    }
}
