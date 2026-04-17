using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InitButton : MonoBehaviour
{
    private GameObject lastselect;
    private bool usingKeyboard = false;

    // Start is called before the first frame update
    void Start()
    {
        lastselect = new GameObject();
    }

    // Update is called once per frame
    void Update()
    {
         if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            usingKeyboard = true;
        }
        if (Input.GetMouseButtonDown(0))
        {
            usingKeyboard = false;
        }
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            // 只有在使用键盘模式时才恢复选中
            if (usingKeyboard && lastselect != null)
            {
                EventSystem.current.SetSelectedGameObject(lastselect);
            }
        }
        else
        {
            lastselect = EventSystem.current.currentSelectedGameObject;
        }
    }
}
