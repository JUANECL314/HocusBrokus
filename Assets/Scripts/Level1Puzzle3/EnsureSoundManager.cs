using UnityEngine;

public class EnsureSoundManager : MonoBehaviour
{
    public GameObject soundManagerPrefab;

    private void Awake()
    {
        Debug.Log("[EnsureSoundManager] Awake called.");

        if (soundManagerPrefab == null)
        {
            Debug.LogError("[EnsureSoundManager] No prefab assigned.");
            return;
        }

        SoundManager existing = FindActiveSoundManager();

        if (existing == null)
        {
            Debug.Log("[EnsureSoundManager] No active SoundManager found → instantiating.");
            GameObject sm = Instantiate(soundManagerPrefab);
            DontDestroyOnLoad(sm);
        }
        else
        {
            Debug.Log("[EnsureSoundManager] Active SoundManager already exists: " + existing.name);
        }
    }

    private SoundManager FindActiveSoundManager()
    {
        SoundManager[] managers = FindObjectsOfType<SoundManager>(true); // includes inactive
        foreach (var m in managers)
        {
            if (m.gameObject.activeInHierarchy)
                return m;
        }
        return null;
    }
}
