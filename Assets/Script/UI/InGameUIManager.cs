using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameUIManager : MonoBehaviour
{
    public GameObject EndGamePanel;

    private NetworkRunner networkRunner;

    public void Start()
    {
        networkRunner = FindObjectOfType<NetworkRunner>();
        if (networkRunner == null)
        {
            UnityEngine.Debug.LogWarning("Network Runner Not Found");
        }
    }

    public void LoadScene(string sceneName)
    {
        networkRunner.LoadScene(sceneName);
    }
}
