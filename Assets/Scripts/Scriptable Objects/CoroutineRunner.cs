
using System.Collections;
using UnityEngine;

public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner instance;

    public static CoroutineRunner Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("CoroutineRunner");
                instance = go.AddComponent<CoroutineRunner>();
                GameObject.DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public void RunCoroutine(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }
}