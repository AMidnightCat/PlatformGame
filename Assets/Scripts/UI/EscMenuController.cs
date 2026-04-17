using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class EscMenuController : MonoBehaviour
{
    public static EscMenuController instance;

    [Header("UI组件")]
    public CanvasGroup canvasGroup;           // 用于控制显示/隐藏
    public GameObject menuPanel;               // 菜单面板
    public Button returnToMenuButton;          // 返回主菜单按钮
    public Button resumeGameButton;            // 继续游戏按钮

    [Header("设置")]
    public KeyCode escKey = KeyCode.Escape;     // ESC键
    public float targetAlpha = 1f;              // 目标透明度

    private bool isShowing = false;
    private InGameSaveUIController saveUI;      // 引用存档UI，用于冲突处理

    void Awake()
    {
        // 单例模式
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            if (instance != this)
            {
                Destroy(gameObject);
            }
        }
    }

    void Start()
    {
        // 获取存档UI的引用
        saveUI = InGameSaveUIController.instance;

        // 初始化UI状态 - 默认隐藏
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // 初始化按钮监听
        InitializeButtons();

        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 新增：重新获取存档UI引用
        saveUI = InGameSaveUIController.instance;

        // 场景切换时自动隐藏菜单
        if (isShowing)
        {
            HideMenu();
        }
    }

    void InitializeButtons()
    {
        if (returnToMenuButton != null)
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (resumeGameButton != null)
            resumeGameButton.onClick.AddListener(HideMenu);
    }

    void Update()
    {
        // 检测ESC键
        if (Input.GetKeyDown(escKey))
        {
            ToggleMenu();
        }
    }

    // 切换菜单显示状态
    void ToggleMenu()
    {
        if (isShowing)
        {
            HideMenu();
        }
        else
        {
            ShowMenu();
        }
    }

    // 显示菜单
    void ShowMenu()
    {
        // 如果存档界面正在显示，先隐藏它
        if (saveUI != null && saveUI.IsShowing())
        {
            saveUI.HideSaveUI();
        }

        isShowing = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = targetAlpha;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // 暂停游戏
        Time.timeScale = 0f;

        Debug.Log("显示ESC菜单");
    }

    // 隐藏菜单
    public void HideMenu()
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

        Debug.Log("隐藏ESC菜单");
    }

    // 返回主菜单
    void ReturnToMainMenu()
    {
        Debug.Log("返回主菜单");

        // 恢复游戏时间Scale（确保主菜单正常）
        Time.timeScale = 1f;

        // 隐藏菜单
        HideMenu();

        // 加载主菜单场景（请替换为你的主菜单场景名）
        SceneManager.LoadScene("MENU");
    }

    // 退出游戏
    void QuitGame()
    {
        Debug.Log("退出游戏");

        // 恢复游戏时间Scale
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // 检查菜单是否显示
    public bool IsShowing()
    {
        return isShowing;
    }
}