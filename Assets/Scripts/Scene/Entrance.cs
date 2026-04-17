using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entrance : MonoBehaviour
{
    public string entranceID;
    void Start()
    {
        if(TestDragonControl.instance.SceneID == entranceID)
        {
            TestDragonControl.instance.transform.position = transform.position;
        }
    }

}
