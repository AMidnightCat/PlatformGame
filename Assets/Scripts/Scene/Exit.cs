using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Exit : MonoBehaviour
{
    public string nextSceneName;
    [SerializeField] private string newSceneID;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.tag == "Player")
        {
            TestDragonControl.instance.SceneID = newSceneID;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
