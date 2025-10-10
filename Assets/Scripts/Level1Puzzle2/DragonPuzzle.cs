using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class DragonPuzzle : MonoBehaviour
{
    public Material fireMaterial; // assign a red/fire material in the Inspector (optional)
    private Material originalMaterial;
    private Renderer rend;
    private bool isOnFire = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
            originalMaterial = rend.material;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        TryIgnite(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        TryIgnite(other.gameObject);
    }

    private void TryIgnite(GameObject other)
    {
        if (isOnFire) return;
        if (other.CompareTag("Fire"))
        {
            Ignite();
            Destroy(other); // optional: remove the projectile
        }
    }

    private void Ignite()
    {
        if (rend == null) return;

        if (fireMaterial != null)
        {
            rend.material = fireMaterial;
        }
        else
        {
            // fallback: tint the current material red
            rend.material.color = Color.red;
        }

        isOnFire = true;
    }

    // Optional: call to revert appearance
    public void Extinguish()
    {
        if (rend == null) return;
        if (originalMaterial != null) rend.material = originalMaterial;
        isOnFire = false;
    }
}