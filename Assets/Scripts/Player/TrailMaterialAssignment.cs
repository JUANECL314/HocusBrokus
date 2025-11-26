using UnityEngine;
using Photon.Pun;

public class TrailMaterialAssignment : MonoBehaviour
{
    [Header("Reference to the Trail Renderer")]
    public TrailRenderer trailRenderer;
    [System.Serializable]
    public class TrailMaterials
    {
        public Material material;
    }

    [Header("Materials for Each Element")]
    public TrailMaterials fireTrail;
    public TrailMaterials waterTrail;
    public TrailMaterials earthTrail;
    public TrailMaterials windTrail;

    private Magic magic;
    private PhotonView view;

    private void Start()
    {
        view = GetComponent<PhotonView>();
        magic = GetComponent<Magic>();

        if (trailRenderer == null)
        {
            Debug.LogError("TrailMaterialAssignment: No TrailRenderer assigned!");
            return;
        }

        // --- Make trails linger smoothly even when stopping ---
        trailRenderer.minVertexDistance = 0.01f;
        trailRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;

        if (view.IsMine)
        {
            if (magic == null || magic.elementSelected == null)
            {
                Debug.LogWarning("TrailMaterialAssignment: Missing Magic or elementSelected");
                return;
            }

            Elements.ElementType type = magic.elementSelected.elementType;

            ApplyTrail(type);
            view.RPC(nameof(RPC_ApplyTrail), RpcTarget.OthersBuffered, (int)type);

            trailRenderer.emitting = true;
            trailRenderer.generateLightingData = true;
        }
    }


    [PunRPC]
    private void RPC_ApplyTrail(int typeInt)
    {
        ApplyTrail((Elements.ElementType)typeInt);
    }

    private void ApplyTrail(Elements.ElementType type)
    {
        Material mat = null;

        switch (type)
        {
            case Elements.ElementType.Fire:
                mat = fireTrail.material;
                ApplyFireStyle();
                break;

            case Elements.ElementType.Water:
                mat = waterTrail.material;
                ApplyWaterStyle();
                break;

            case Elements.ElementType.Earth:
                mat = earthTrail.material;
                ApplyEarthStyle();
                break;

            case Elements.ElementType.Wind:
                mat = windTrail.material;
                ApplyWindStyle();
                break;

            default:
                Debug.LogWarning("TrailMaterialAssignment: Unknown element type.");
                return;
        }

        if (mat != null)
        {
            trailRenderer.material = mat;
            BoostEmission(mat, 4f);
        }

        Debug.Log($"Trail applied → {type}");
    }

    private void BoostEmission(Material mat, float intensity)
    {
        if (mat.HasProperty("_EmissionColor"))
        {
            Color original = mat.GetColor("_EmissionColor");
            mat.SetColor("_EmissionColor", original * intensity);
        }
    }

    // ------------------------------------------------
    // TRAIL STYLES
    // ------------------------------------------------

    private void ApplyFireStyle()
    {
        trailRenderer.time = 3f;
        trailRenderer.widthMultiplier = 5f;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0f),
                new GradientColorKey(new Color(1f, 0.2f, 0f), 0.4f),
                new GradientColorKey(new Color(0.5f, 0f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        trailRenderer.colorGradient = g;
    }

    private void ApplyWaterStyle()
    {
        trailRenderer.time = 2.2f;
        trailRenderer.widthMultiplier = 4f;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0f, 0.6f, 1f), 0f),
                new GradientColorKey(new Color(0f, 0.8f, 1f), 0.5f),
                new GradientColorKey(new Color(0f, 0.4f, 0.9f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.5f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        trailRenderer.colorGradient = g;
    }

    private void ApplyEarthStyle()
    {
        trailRenderer.time = 1.8f;
        trailRenderer.widthMultiplier = 5f;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.35f, 0.25f, 0.05f), 0f),
                new GradientColorKey(new Color(0.25f, 0.18f, 0.03f), 0.5f),
                new GradientColorKey(new Color(0.1f, 0.08f, 0.02f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.6f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        trailRenderer.colorGradient = g;
    }

    private void ApplyWindStyle()
    {
        trailRenderer.time = 1.6f;
        trailRenderer.widthMultiplier = 4f;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.85f, 0.9f, 1f), 0.5f),
                new GradientColorKey(new Color(1f, 1f, 1f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.4f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        trailRenderer.colorGradient = g;
    }
}
