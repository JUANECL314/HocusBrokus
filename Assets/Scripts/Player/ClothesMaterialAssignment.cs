using UnityEngine;
using Photon.Pun;

public class ClothesMaterialAssignment : MonoBehaviour
{
    [Header("Body Part Mesh Renderers (assign in inspector)")]
    public SkinnedMeshRenderer hatRenderer;
    public SkinnedMeshRenderer shirtRenderer;
    public SkinnedMeshRenderer legsRenderer;
    public SkinnedMeshRenderer rightShoeRenderer;
    public SkinnedMeshRenderer leftShoeRenderer;

    [System.Serializable]
    public class ElementMaterials
    {
        public Material hat;
        public Material shirt;
        public Material legs;
        public Material rightShoe;
        public Material leftShoe;
    }

    [Header("Materials per Element Type")]
    public ElementMaterials fireMaterials;
    public ElementMaterials waterMaterials;
    public ElementMaterials earthMaterials;
    public ElementMaterials windMaterials;

    private Magic magic;
    private PhotonView view;

    private void Start()
    {
        view = GetComponent<PhotonView>();
        magic = GetComponent<Magic>();

        if (magic == null)
        {
            Debug.LogWarning("ClothesMaterialAssignment: Magic script missing!");
            return;
        }

        if (view.IsMine)
        {
            // local player chooses element → broadcast it
            if (magic.elementSelected != null)
            {
                int typeInt = (int)magic.elementSelected.elementType;
                view.RPC(nameof(RPC_ApplyElement), RpcTarget.AllBuffered, typeInt);
            }
        }
    }

    [PunRPC]
    private void RPC_ApplyElement(int elementIndex)
    {
        Elements.ElementType type = (Elements.ElementType)elementIndex;
        ApplyMaterials(type);
    }

    private void ApplyMaterials(Elements.ElementType type)
    {
        ElementMaterials mats = null;

        switch (type)
        {
            case Elements.ElementType.Fire:
                mats = fireMaterials;
                break;
            case Elements.ElementType.Water:
                mats = waterMaterials;
                break;
            case Elements.ElementType.Earth:
                mats = earthMaterials;
                break;
            case Elements.ElementType.Wind:
                mats = windMaterials;
                break;
            default:
                Debug.LogWarning("No materials assigned for this element type.");
                return;
        }

        if (mats == null)
            return;

        if (hatRenderer != null && mats.hat != null) hatRenderer.material = mats.hat;
        if (shirtRenderer != null && mats.shirt != null) shirtRenderer.material = mats.shirt;
        if (legsRenderer != null && mats.legs != null) legsRenderer.material = mats.legs;
        if (rightShoeRenderer != null && mats.rightShoe != null) rightShoeRenderer.material = mats.rightShoe;
        if (leftShoeRenderer != null && mats.leftShoe != null) leftShoeRenderer.material = mats.leftShoe;

        Debug.Log($"Applied materials for element: {type}");
    }
}
