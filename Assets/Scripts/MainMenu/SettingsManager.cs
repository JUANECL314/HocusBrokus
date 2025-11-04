using System;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager I { get; private set; }

    public float MouseSensitivity { get; private set; } = 1.5f;
    public float GamepadSensitivity { get; private set; } = 0.7f;
    public bool InvertY { get; private set; } = false;

    public event Action OnChanged;

    const string K_MS = "cfg_mouse_sens";
    const string K_GS = "cfg_gamepad_sens";
    const string K_IY = "cfg_invert_y";

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void SetMouseSensitivity(float v) { MouseSensitivity = Mathf.Clamp(v, 0.05f, 10f); SaveAndNotify(); }
    public void SetGamepadSensitivity(float v) { GamepadSensitivity = Mathf.Clamp(v, 0.05f, 10f); SaveAndNotify(); }
    public void SetInvertY(bool v) { InvertY = v; SaveAndNotify(); }

    void SaveAndNotify()
    {
        PlayerPrefs.SetFloat(K_MS, MouseSensitivity);
        PlayerPrefs.SetFloat(K_GS, GamepadSensitivity);
        PlayerPrefs.SetInt(K_IY, InvertY ? 1 : 0);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }

    void Load()
    {
        if (PlayerPrefs.HasKey(K_MS)) MouseSensitivity = PlayerPrefs.GetFloat(K_MS, MouseSensitivity);
        if (PlayerPrefs.HasKey(K_GS)) GamepadSensitivity = PlayerPrefs.GetFloat(K_GS, GamepadSensitivity);
        if (PlayerPrefs.HasKey(K_IY)) InvertY = PlayerPrefs.GetInt(K_IY, 0) == 1;
    }
}
