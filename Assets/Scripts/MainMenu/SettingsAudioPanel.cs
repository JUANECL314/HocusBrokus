// SettingsAudioPanel.cs
using UnityEngine;
using UnityEngine.UI;

public class SettingsAudioPanel : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;

    void OnEnable()
    {
        // Lee estado real para que la UI refleje lo que está en el mixer/PlayerPrefs
        float master01 = 0.8f;
        float sfx01 = 0.8f;

        // Si SoundManager ya aplicó dB antes, no podemos leer dB→01 fácilmente del mixer
        // así que preferimos cachear en PlayerPrefs:
        if (PlayerPrefs.HasKey("MasterVol"))
        {
            // almacenaste dB, lo reaproximamos a 0..1 (opcional simple):
            float dB = PlayerPrefs.GetFloat("MasterVol", -80f);
            master01 = dB > -80f ? Mathf.Clamp01(Mathf.Pow(10f, dB / 20f)) : 0f;
        }
        if (PlayerPrefs.HasKey("SFXVol"))
        {
            float dB = PlayerPrefs.GetFloat("SFXVol", -80f);
            sfx01 = dB > -80f ? Mathf.Clamp01(Mathf.Pow(10f, dB / 20f)) : 0f;
        }

        if (masterSlider) masterSlider.SetValueWithoutNotify(master01);
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(sfx01);
    }

    public void OnMasterChanged(float v01)
    {
        if (SoundManager.Instance) SoundManager.Instance.SetMasterVolume01(v01); // guarda y aplica dB
    }

    public void OnSfxChanged(float v01)
    {
        if (SoundManager.Instance) SoundManager.Instance.SetSfxVolume01(v01); // guarda y aplica dB
    }
}
