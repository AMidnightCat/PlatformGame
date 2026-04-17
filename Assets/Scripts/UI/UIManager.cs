using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("UI Controllers")]
    public InGameSaveUIController saveUIController;
    public EscMenuController escMenuController;

    [Header("UI根对象")]
    public GameObject uiRootCanvas; // 整个UI Canvas根对象

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // UIManager是根对象

            // 如果UI Canvas是子物体，也需要设置为不销毁
            if (uiRootCanvas != null)
            {
                DontDestroyOnLoad(uiRootCanvas);
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 初始查找UI控制器
        FindUIControllers();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"UIManager: 场景加载完成 - {scene.name}");

        // 重新查找UI控制器（可能在场景中有新的引用）
        FindUIControllers();

    }

    void FindUIControllers()
    {
        // 如果控制器丢失，在当前物体或子物体中查找
        if (saveUIController == null)
            saveUIController = GetComponentInChildren<InGameSaveUIController>();

        if (escMenuController == null)
            escMenuController = GetComponentInChildren<EscMenuController>();

        Debug.Log($"UIManager: 找到存档控制器 - {saveUIController != null}, ESC控制器 - {escMenuController != null}");
    }

}