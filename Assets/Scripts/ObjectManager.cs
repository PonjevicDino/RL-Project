using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour {

    [SerializeField]
    private GameObject[] objectPrefabs;

    private List<GameObject> pooledObjs = new List<GameObject>();

    private GameObject Generate(string type, bool isActive, bool spawn) {
        for(int i = 0; i < objectPrefabs.Length; i++) {
            if(objectPrefabs[i].name.Equals(type)) {
                if (!spawn)
                {
                    return objectPrefabs[i];
                }
                GameObject newObject = Instantiate(objectPrefabs[i]);
                newObject.SetActive(isActive);
                pooledObjs.Add(newObject);
                newObject.name = type;
                return newObject;
            }
        }
        return null;
        }

    public GameObject GetObject(string type, bool spawn = true) {
        foreach(GameObject obj in pooledObjs) {
            if(obj.name.Equals(type) && !obj.activeInHierarchy) {
                obj.SetActive(true);
                return obj;
            }
        }

        return Generate(type, true, spawn);
    }
}