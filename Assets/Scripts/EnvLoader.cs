using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnvLoader : MonoBehaviour
{
    [SerializeField] private GameObject envPrefab;
    [SerializeField] public int envCount;

    void OnValidate()
    {
        envCount = Mathf.Max(1, envCount);    
    }

    void Start()
    {
        int startHeight = ((envCount - 1) * 100) / 2 * -1;

        for (int env = 0; env < envCount; env++)
        {
            GameObject newEnv = Instantiate(envPrefab, new Vector3(0.0f, startHeight + 100 * env, 0.0f), Quaternion.identity, this.transform.parent);
            newEnv.name = "Env_" + env.ToString("00");
        }

        var cameraGameObjects = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "Main Camera");

        int iteration = 0;
        foreach(var camera in cameraGameObjects)
        {
            camera.GetComponent<Camera>().rect = new Rect(1.0f / envCount * iteration, 0.0f, 1.0f / envCount, 1.0f);
            iteration++;
        }
    }
}
