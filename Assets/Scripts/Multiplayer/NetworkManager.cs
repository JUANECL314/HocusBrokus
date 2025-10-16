using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    //Cantidad de jugadores por sala
    public int maxPlayer = 4;
    //Objeto singleton
    public static NetworkManager instance;
    private void Awake()
    {
        
        if (instance == null )
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
    }


    
}
