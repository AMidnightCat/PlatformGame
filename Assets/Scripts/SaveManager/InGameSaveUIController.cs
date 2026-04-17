using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameSaveUIController : MonoBehaviour
{
    public static InGameSaveUIController instance;

    [Header("UI组件")]
    public CanvasGroup canvasGroup;           // 用于控制透明度
    public GameObject savePanel;               // 存档面板
    public Button[] saveButtons = new Button[15];  // 15个存档按钮
    public TextMeshProUGUI[] buttonTexts = new TextMeshProUGUI[15];  // 按钮文本
    public Button closeButton;                 // 关闭按钮

    private SaveManager saveManager;
    private GameDataManager gameDataManager;
    private bool isShowing = false;
    private int selectedSlotIndex = -1;

    // 引用玩家组件
    private GameObject player;

    void Awake()
    {
        // 单例模式，确保只有一个实例
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);  // 如果已存在，销毁新实例
            return;
        }
    }

    void Start()
    {
        saveManager = SaveManager.instance;
        gameDataManager = GameDataManager.instance;

        // 初始化UI状态
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // 初始化按钮监听
        InitializeButtons();

        // 初始时隐藏
        HideSaveUI();

        // 初始查找玩家
        FindPlayer();

    }

    void OnDestroy()
    {
        // 取消订阅场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 场景加载完成时的回调
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"场景加载完成：{scene.name}，重新查找玩家组件");

        // 重新获取管理器引用
        saveManager = SaveManager.instance;
        gameDataManager = GameDataManager.instance;

        // 重新查找玩家
        FindPlayer();

        if (saveManager != null)
        {
            RefreshSaveButtons();
        }

        // 如果存档界面是显示的，但场景切换了，自动隐藏
        if (isShowing)
        {
            HideSaveUI();
        }

    }

    // 查找玩家对象和组件
    void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void InitializeButtons()
    {
        // 为每个存档按钮添加点击事件
        for (int i = 0; i < saveButtons.Length; i++)
        {
            int index = i;
            if (saveButtons[i] != null)
            {
                saveButtons[i].onClick.AddListener(() => OnSaveButtonClick(index));
            }
        }

        // 关闭按钮
        if (closeButton != null)
            closeButton.onClick.AddListener(HideSaveUI);
    }

    // 显示存档界面
    public void ShowSaveUI()
    {
        isShowing = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.9f;           // 直接设置目标透明度
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        RefreshSaveButtons();
        UpdateLocationAndTime();

        // 暂停游戏
        Time.timeScale = 0f;

        Debug.Log("显示存档界面");
    }

    // 隐藏存档界面
    public void HideSaveUI()
    {
        isShowing = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // 恢复游戏
        Time.timeScale = 1f;

        Debug.Log("隐藏存档界面");
    }

    // 刷新存档按钮
    void RefreshSaveButtons()
    {
        List<SaveManager.SlotInfo> slotInfos = saveManager.GetAllSlotInfo();

        for (int i = 0; i < saveButtons.Length; i++)
        {
            if (i < slotInfos.Count)
            {
                UpdateButtonDisplay(i, slotInfos[i]);
            }
        }
    }

    // 更新按钮显示
    void UpdateButtonDisplay(int slotIndex, SaveManager.SlotInfo info)
    {
        if (buttonTexts[slotIndex] == null) return;

        if (info.hasData)
        {
            // 有存档：显示预览信息
            buttonTexts[slotIndex].text = info.previewInfo;
            buttonTexts[slotIndex].color = Color.white;
            buttonTexts[slotIndex].fontSize = 20; // 游戏内可以小一点
        }
        else
        {
            // 空存档
            buttonTexts[slotIndex].text = $"存档 {slotIndex + 1}\n空存档";
            buttonTexts[slotIndex].color = Color.gray;
            buttonTexts[slotIndex].fontSize = 20;
        }
    }

    // 更新位置和时间信息
    void UpdateLocationAndTime()
    {
        if (gameDataManager.CurrentGameData != null)
        {
            float playTime = gameDataManager.CurrentGameData.playTime;
        }
    }

    // 存档按钮点击事件
    void OnSaveButtonClick(int slotIndex)
    {
        selectedSlotIndex = slotIndex;

        bool hasData = saveManager.IsSlotOccupied(slotIndex);

        if (hasData)
        {
            // 已有存档，显示覆盖确认
            ShowOverwriteConfirmPanel(slotIndex);
        }
        else
        {
            // 空存档，直接保存
            SaveGameToSlot(slotIndex);
        }
    }

    // 显示覆盖确认
    void ShowOverwriteConfirmPanel(int slotIndex)
    {
        SaveData data = saveManager.GetSlotData(slotIndex);

        // 这里可以调用一个简单的确认面板
        Debug.Log($"存档 {slotIndex + 1} 已有数据，是否覆盖？");

        // 简单起见，这里直接覆盖，您可以根据需要添加确认面板
        SaveGameToSlot(slotIndex);
    }

    // 保存游戏到指定槽位
    void SaveGameToSlot(int slotIndex)
    {
        if (gameDataManager.CurrentGameData == null)
        {
            Debug.LogError("没有当前游戏数据");
            return;
        }

        // 更新当前游戏数据
        UpdateCurrentGameData();

        // 设置存档槽位
        gameDataManager.CurrentGameData.saveSlot = slotIndex;

        // 保存
        if (saveManager.SaveGame(slotIndex, gameDataManager.CurrentGameData))
        {
            Debug.Log($"游戏保存到存档 {slotIndex + 1} 成功");

            // 刷新按钮显示
            RefreshSaveButtons();

            // 可以显示一个保存成功的提示
            ShowSaveSuccessMessage();
        }
        else
        {
            Debug.LogError("保存失败");
        }
    }

    // 更新当前游戏数据
    void UpdateCurrentGameData()
    {
        if (gameDataManager.CurrentGameData == null) return;

        // 更新场景名称
        gameDataManager.CurrentGameData.currentSceneName = SceneManager.GetActiveScene().name;

        // 更新玩家位置和血量（需要找到玩家对象）
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            gameDataManager.CurrentGameData.playerPosition = player.transform.position;

            // 如果有PlayerHealth组件，更新血量
            /*
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                gameDataManager.CurrentGameData.playerHealth = health.currentHealth;
                gameDataManager.CurrentGameData.playerMaxHealth = health.maxHealth;
            }
            */
        }
    }

    // 显示保存成功消息
    void ShowSaveSuccessMessage()
    {
        Debug.Log("保存成功");
    }

    // 检查UI是否正在显示
    public bool IsShowing()
    {
        return isShowing;
    }
}