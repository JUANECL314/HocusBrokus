using System.Collections;
using UnityEngine;

public class SearchObject : MonoBehaviour
{
    [SerializeField]
    string gameObjectToUse = null;
    [SerializeField]
    GameObject sceneObject = null;

     void Start()
    {
        StartCoroutine(buscarObjeto());
    }


    IEnumerator buscarObjeto ()
    {
        yield return new WaitForSeconds(2f);
        sceneObject = GameObject.Find(gameObjectToUse+"(Clone)");

        if (sceneObject == null )
        {
            StartCoroutine(buscarObjeto());
        }

    }
}
