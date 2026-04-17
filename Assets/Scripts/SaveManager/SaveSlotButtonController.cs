using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;  // 添加TMP命名空间
using UnityEngine.SceneManagement;

public class SaveSlotButtonController : MonoBehaviour
{
    [Header("按钮配置")]
    public Button[] saveButtons = new Button[15];
    public TextMeshProUGUI[] buttonTexts = new TextMeshProUGUI[15]; 

    [Header("操作按钮")]
    public Button loadModeButton;      // 加载存档模式按钮（默认模式）
    public Button newGameButton;       // 新游戏按钮
    public Button deleteButton;        // 删除游戏按钮
    public Button backButton;          // 返回菜单按钮

    [Header("确认/信息面板")]
    public GameObject confirmPanel;     // 确认面板
    public TextMeshProUGUI confirmTitleText;       // 面板标题
    public TextMeshProUGUI confirmMessageText;     // 面板内容
    public Button confirmYesButton;     // 确认按钮
    public Button confirmNoButton;      // 取消按钮

    [Header("模式指示")]
    public TextMeshProUGUI currentModeText;        // 当前模式

    public string startScene;
    private SaveManager saveManager;
    private GameDataManager gameDataManager;
    private int selectedSlotIndex = -1;

    // 操作模式
    private enum OperationMode
    {
        Load,       // 加载存档模式（默认）
        Delete      // 删除模式
    }
    private OperationMode currentMode = OperationMode.Load;

    // 待确认的操作
    private enum PendingAction
    {
        None,
        NewGame,    // 新游戏（不选存档位置）
        LoadGame,   // 加载存档（需要选存档位置）
        DeleteSave  // 删除存档（需要选存档位置）
    }
    private PendingAction pendingAction = PendingAction.None;
    private int pendingSlotIndex = -1;

    void Start()
    {
        saveManager = SaveManager.instance;
        gameDataManager = GameDataManager.instance;

        InitializeUI();
        RefreshAllSaveButtons();
        AddButtonListeners();

        // 默认为加载模式
        SetMode(OperationMode.Load);
    }

    void InitializeUI()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    void AddButtonListeners()
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

        // 操作按钮
        if (loadModeButton != null)
            loadModeButton.onClick.AddListener(() => SetMode(OperationMode.Load));

        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameButtonClick);

        if (deleteButton != null)
            deleteButton.onClick.AddListener(() => SetMode(OperationMode.Delete));

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClick);

        // 确认面板按钮
        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(OnConfirmYes);

        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(OnConfirmNo);
    }

    // 设置当前模式
    void SetMode(OperationMode mode)
    {
        currentMode = mode;
        selectedSlotIndex = -1;

        // 更新模式显示
        if (currentModeText != null)
        {
            switch (mode)
            {
                case OperationMode.Load:
                    currentModeText.text = "当前模式：加载存档";
                    currentModeText.color = Color.green;
                    break;
                case OperationMode.Delete:
                    currentModeText.text = "当前模式：删除存档";
                    currentModeText.color = Color.red;
                    break;
            }
        }

        // 重置高亮
        HighlightSelectedButton(-1);

        Debug.Log($"切换到模式：{mode}");
    }

    // 刷新所有存档按钮
    public void RefreshAllSaveButtons()
    {
        List<SaveManager.SlotInfo> slotInfos = saveManager.GetAllSlotInfo();

        for (int i = 0; i < saveButtons.Length; i++)
        {
            UpdateButtonDisplay(i, slotInfos[i]);
        }
    }

    // 更新单个按钮显示
    void UpdateButtonDisplay(int slotIndex, SaveManager.SlotInfo info)
    {
        if (buttonTexts[slotIndex] == null) return;

        if (info.hasData)
        {
            // 有存档：显示预览信息（存档编号 | 场景名 | 最后存档时间）
            buttonTexts[slotIndex].text = info.previewInfo;
            buttonTexts[slotIndex].color = Color.white;
            buttonTexts[slotIndex].fontSize = 24;

            Image buttonImage = saveButtons[slotIndex].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = new Color(0.8f, 0.9f, 1f);
            }
        }
        else
        {
            // 空存档
            buttonTexts[slotIndex].text = "空存档";
            buttonTexts[slotIndex].color = Color.gray;
            buttonTexts[slotIndex].fontSize = 30;

            Image buttonImage = saveButtons[slotIndex].GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = Color.white;
            }
        }
    }

    // 存档按钮点击事件
    void OnSaveButtonClick(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        HighlightSelectedButton(slotIndex);

        bool hasData = saveManager.IsSlotOccupied(slotIndex);

        // 根据当前模式处理点击
        switch (currentMode)
        {
            case OperationMode.Load:
                if (hasData)
                {
                    // 有存档：显示加载确认
                    ShowLoadConfirmPanel(slotIndex);
                }
                else
                {
                    // 空存档：提示不能加载
                    ShowMessagePanel("提示", "该存档位为空，无法加载");
                }
                break;

            case OperationMode.Delete:
                if (hasData)
                {
                    // 有存档：显示删除确认
                    ShowDeleteConfirmPanel(slotIndex);
                }
                else
                {
                    // 空存档：提示不能删除
                    ShowMessagePanel("提示", "空存档无法删除");
                }
                break;
        }
    }

    // 新游戏按钮点击事件
    void OnNewGameButtonClick()
    {
        // 显示新游戏确认面板（不需要选择存档位置）
        ShowNewGameConfirmPanel();
    }

    #region 确认面板显示方法

    // 显示新游戏确认面板（不需要存档槽参数）
    void ShowNewGameConfirmPanel()
    {
        confirmTitleText.text = "新游戏";
        confirmMessageText.text = "确定要开始新游戏吗？";

        pendingAction = PendingAction.NewGame;
        pendingSlotIndex = -1;

        // 确保两个按钮都显示
        if (confirmNoButton != null)
            confirmNoButton.gameObject.SetActive(true);
        if (confirmYesButton != null)
        {
            // 获取确认按钮上的TMP文本
            TextMeshProUGUI yesButtonText = confirmYesButton.GetComponentInChildren<TextMeshProUGUI>();
            if (yesButtonText != null)
                yesButtonText.text = "确认";
        }

        confirmPanel.SetActive(true);
    }

    // 显示加载确认面板
    void ShowLoadConfirmPanel(int slotIndex)
    {
        SaveData data = saveManager.GetSlotData(slotIndex);
        if (data == null) return;

        string sceneName = string.IsNullOrEmpty(data.currentSceneName) ? "未知场景" : data.currentSceneName;

        confirmTitleText.text = "加载存档";
        confirmMessageText.text = $"确定要从存档 {slotIndex + 1} 继续游戏吗？\n\n" +
                                 $"存档时间：{data.saveTime:yyyy-MM-dd HH:mm:ss}\n" +
                                 $"场景：{sceneName}\n" +
                                 $"血量：{data.playerHealth}/{data.playerMaxHealth}\n" +
                                 $"游戏时间：{FormatPlayTime(data.playTime)}";

        pendingAction = PendingAction.LoadGame;
        pendingSlotIndex = slotIndex;

        // 确保两个按钮都显示
        if (confirmNoButton != null)
            confirmNoButton.gameObject.SetActive(true);
        if (confirmYesButton != null)
        {
            TextMeshProUGUI yesButtonText = confirmYesButton.GetComponentInChildren<TextMeshProUGUI>();
            if (yesButtonText != null)
                yesButtonText.text = "确认";
        }

        confirmPanel.SetActive(true);
    }

    // 显示删除确认面板
    void ShowDeleteConfirmPanel(int slotIndex)
    {
        SaveData data = saveManager.GetSlotData(slotIndex);
        if (data == null) return;

        string sceneName = string.IsNullOrEmpty(data.currentSceneName) ? "未知场景" : data.currentSceneName;

        confirmTitleText.text = "确认删除";
        confirmMessageText.text = $"确定要删除存档 {slotIndex + 1} 吗？\n\n" +
                                 $"存档时间：{data.saveTime:yyyy-MM-dd HH:mm:ss}\n" +
                                 $"场景：{sceneName}\n" +
                                 $"血量：{data.playerHealth}/{data.playerMaxHealth}\n\n" +
                                 $"⚠️ 此操作不可恢复！";

        pendingAction = PendingAction.DeleteSave;
        pendingSlotIndex = slotIndex;

        // 确保两个按钮都显示
        if (confirmNoButton != null)
            confirmNoButton.gameObject.SetActive(true);
        if (confirmYesButton != null)
        {
            TextMeshProUGUI yesButtonText = confirmYesButton.GetComponentInChildren<TextMeshProUGUI>();
            if (yesButtonText != null)
                yesButtonText.text = "确认";
        }

        confirmPanel.SetActive(true);
    }

    // 显示普通消息面板
    void ShowMessagePanel(string title, string message)
    {
        confirmTitleText.text = title;
        confirmMessageText.text = message;

        pendingAction = PendingAction.None;

        // 纯消息面板只显示确认按钮，隐藏取消按钮
        if (confirmYesButton != null)
        {
            TextMeshProUGUI yesButtonText = confirmYesButton.GetComponentInChildren<TextMeshProUGUI>();
            if (yesButtonText != null)
                yesButtonText.text = "确定";
            confirmYesButton.gameObject.SetActive(true);
        }
        if (confirmNoButton != null)
            confirmNoButton.gameObject.SetActive(false);

        confirmPanel.SetActive(true);
    }

    #endregion

    #region 确认面板按钮事件

    void OnConfirmYes()
    {
        // 恢复按钮状态
        if (confirmNoButton != null)
            confirmNoButton.gameObject.SetActive(true);
        if (confirmYesButton != null)
        {
            TextMeshProUGUI yesButtonText = confirmYesButton.GetComponentInChildren<TextMeshProUGUI>();
            if (yesButtonText != null)
                yesButtonText.text = "确认";
        }

        switch (pendingAction)
        {
            case PendingAction.NewGame:
                StartNewGame();
                break;
            case PendingAction.LoadGame:
                LoadGame(pendingSlotIndex);
                break;
            case PendingAction.DeleteSave:
                DeleteSave(pendingSlotIndex);
                break;
            case PendingAction.None:
                // 纯消息面板，直接关闭
                break;
        }

        // 关闭确认面板
        confirmPanel.SetActive(false);
        pendingAction = PendingAction.None;
        pendingSlotIndex = -1;
    }

    void OnConfirmNo()
    {
        // 恢复按钮状态
        if (confirmNoButton != null)
            confirmNoButton.gameObject.SetActive(true);
        if (confirmYesButton != null)
        {
            TextMeshProUGUI yesButtonText = confirmYesButton.GetComponentInChildren<TextMeshProUGUI>();
            if (yesButtonText != null)
                yesButtonText.text = "确认";
        }

        confirmPanel.SetActive(false);
        pendingAction = PendingAction.None;
        pendingSlotIndex = -1;

        // 取消后重置选中状态
        if (currentMode == OperationMode.Delete)
        {
            selectedSlotIndex = -1;
            HighlightSelectedButton(-1);
        }
    }

    #endregion

    #region 实际操作

    void StartNewGame()
    {
        Debug.Log("开始新游戏");

        // 创建一个临时的新游戏数据（使用槽位-1表示未保存）
        SaveData newGameData = SaveData.CreateNewSave(-1);

        // 使用GameDataManager中设置的默认起始位置和场景
        newGameData.currentSceneName = gameDataManager.defaultStartScene;
        newGameData.playerPosition = gameDataManager.defaultStartPosition;
        newGameData.playerHealth = 3;
        newGameData.playerMaxHealth = 3;

        // 设置到GameDataManager
        gameDataManager.LoadGameData(newGameData);

        // 加载游戏场景
        SceneManager.LoadScene(startScene);
    }

    void LoadGame(int slotIndex)
    {
        Debug.Log($"从存档槽 {slotIndex + 1} 加载游戏");

        SaveData loadedData = saveManager.LoadGame(slotIndex);

        if (loadedData != null)
        {
            gameDataManager.LoadGameData(loadedData);
            PlayerPrefs.SetInt("LastSaveSlot", slotIndex);

            // 加载存档中的场景
            if (!string.IsNullOrEmpty(loadedData.currentSceneName) && loadedData.currentSceneName != "MENU")
            {
                SceneManager.LoadScene(loadedData.currentSceneName);
            }
            else
            {
                SceneManager.LoadScene(startScene);
            }
        }
        else
        {
            ShowMessagePanel("错误", "加载失败！");
        }
    }

    void DeleteSave(int slotIndex)
    {
        Debug.Log($"删除存档槽 {slotIndex + 1}");

        if (saveManager.DeleteSave(slotIndex))
        {
            RefreshAllSaveButtons();

            if (selectedSlotIndex == slotIndex)
            {
                selectedSlotIndex = -1;
                HighlightSelectedButton(-1);
            }

            ShowMessagePanel("成功", "存档删除成功！");
        }
        else
        {
            ShowMessagePanel("错误", "删除失败！");
        }
    }

    #endregion

    // 高亮选中的按钮
    void HighlightSelectedButton(int selectedIndex)
    {
        for (int i = 0; i < saveButtons.Length; i++)
        {
            if (saveButtons[i] != null)
            {
                Image buttonImage = saveButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    if (i == selectedIndex)
                    {
                        // 选中状态：根据模式显示不同颜色
                        switch (currentMode)
                        {
                            case OperationMode.Load:
                                buttonImage.color = new Color(0.5f, 1f, 0.5f); // 绿色
                                break;
                            case OperationMode.Delete:
                                buttonImage.color = new Color(1f, 0.5f, 0.5f); // 红色
                                break;
                        }
                    }
                    else
                    {
                        // 非选中状态：根据是否有存档显示不同颜色
                        bool hasData = saveManager.IsSlotOccupied(i);
                        buttonImage.color = hasData ? new Color(0.8f, 0.9f, 1f) : Color.white;
                    }
                }
            }
        }
    }

    // 返回按钮点击
    void OnBackButtonClick()
    {
        Debug.Log("返回主菜单");
        SceneManager.LoadScene("MENU");
    }

    // 格式化游戏时间
    string FormatPlayTime(float totalSeconds)
    {
        int hours = Mathf.FloorToInt(totalSeconds / 3600);
        int minutes = Mathf.FloorToInt((totalSeconds % 3600) / 60);

        if (hours > 0)
            return $"{hours}小时{minutes}分钟";
        else
            return $"{minutes}分钟";
    }

    // 当脚本启用时刷新
    void OnEnable()
    {
        if (saveManager != null)
        {
            RefreshAllSaveButtons();
            selectedSlotIndex = -1;
            HighlightSelectedButton(-1);

            // 重置为加载模式
            SetMode(OperationMode.Load);
        }
    }
}