using UnityEngine;
using UnityEngine.SceneManagement;
public class EliminatePlayer : MonoBehaviour
{

    string escenaPermitida = "Lobby";

    string nombreEscena;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        nombreEscena = SceneManager.GetActiveScene().name;
    }

    // Update is called once per frame
    void Update()
    {
        if(escenaPermitida != nombreEscena)
        {
            Destroy(gameObject);
        }
    }
}
 