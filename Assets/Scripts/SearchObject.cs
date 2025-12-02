using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SearchObject : MonoBehaviour
{
    [Header("Puzzle Setup")]
    [Tooltip("Tag used for all MirrorPillar prefabs")]
    [SerializeField] private string mirrorTag = "MirrorPillar";

    [Tooltip("Assign the puzzle manager that will control mirrors")]
    [SerializeField] private MagicPillarPuzzleManager puzzleManager;

    [Tooltip("Delay between search attempts in seconds")]
    [SerializeField] private float searchDelay = 0.5f;

    // Mirrors we have found so far
    private List<MirrorController> foundMirrors = new List<MirrorController>();

    void Start()
    {
        StartCoroutine(FindMirrorsCoroutine());
    }

    private IEnumerator FindMirrorsCoroutine()
    {
        while (foundMirrors.Count == 0)
        {
            // Find all objects with the MirrorPillar tag
            GameObject[] mirrorsInScene = GameObject.FindGameObjectsWithTag(mirrorTag);

            foreach (var obj in mirrorsInScene)
            {
                MirrorController mirror = obj.GetComponent<MirrorController>();

                // Add to our list if not already present
                if (mirror != null && !foundMirrors.Contains(mirror))
                {
                    foundMirrors.Add(mirror);
                    Debug.Log($"Mirror {mirror.name} found and registered.");
                }
            }

            if (foundMirrors.Count > 0)
            {
                // Register mirrors with the puzzle manager
                foreach (var mirror in foundMirrors)
                {
                    if (!puzzleManager.mirrorsToAlign.Contains(mirror))
                        puzzleManager.mirrorsToAlign.Add(mirror);
                }

                Debug.Log($"SearchObject: {foundMirrors.Count} mirror(s) registered with puzzle manager.");
                break; // stop searching once mirrors are found and registered
            }

            yield return new WaitForSeconds(searchDelay);
        }
    }
}
