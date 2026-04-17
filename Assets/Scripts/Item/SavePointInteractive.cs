using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CubeInteractive;

public class SavePointInteractive : MonoBehaviour
{
    [SerializeField] private GameObject ShowButton;
    [SerializeField] private KeyCode InteractiveKey = KeyCode.X;

    private InGameSaveUIController saveUI;
    private bool _isInTrigger = false;

    void Start()
    {
        // 查找游戏内的存档UI
        saveUI = FindObjectOfType<InGameSaveUIController>();

        // 隐藏交互提示
        if (ShowButton != null)
            ShowButton.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(InteractiveKey) && _isInTrigger)
        {
            saveUI.ShowSaveUI();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 持续在触发器内
        ShowButton.SetActive(true);
        _isInTrigger = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        ShowButton.SetActive(false);
        _isInTrigger = false;
    }

}
