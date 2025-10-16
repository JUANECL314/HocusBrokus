using UnityEngine;

public class Trash : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Destroy trash only when hit by a projectile tagged "Wind"
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wind"))
        {
            Destroy(gameObject);
        }
    }

    // In case colliders are not triggers in your setup
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Wind"))
        {
            Destroy(gameObject);
        }
    }
}