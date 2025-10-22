using System.Collections;
using UnityEngine;

public class SearchObject : MonoBehaviour
{
    [SerializeField]
    string gameObjectToUse = null;
    GameObject sceneObject = null;

     void Start()
    {
        StartCoroutine(buscarObjeto());
    }


    IEnumerator buscarObjeto ()
    {
        yield return new WaitForSeconds(2f);
        sceneObject = GameObject.Find(gameObjectToUse);

        if (sceneObject == null )
        {
            StartCoroutine(buscarObjeto());
        }

    }
}
