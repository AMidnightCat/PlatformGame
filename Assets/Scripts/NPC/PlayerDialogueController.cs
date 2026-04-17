using UnityEngine;

public class PlayerDialogueController : MonoBehaviour
{
    public static PlayerDialogueController Instance;

    [Header("主角信息")]
    public string playerName = "冒险者";
    public Sprite playerPortrait;

    [Header("对话设置")]
    public KeyCode interactKey = KeyCode.E;

    private DialogueUIManager dialogueUIManager;
    private bool isInDialogue = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        dialogueUIManager = DialogueUIManager.Instance;
    }

    void Update()
    {
        // 对话中按取消键可以关闭对话（可选）
        if (isInDialogue && Input.GetKeyDown(KeyCode.Escape))
        {
            if (dialogueUIManager != null)
            {
                dialogueUIManager.HideDialogue();
                isInDialogue = false;
            }
        }
    }

    // 开始对话
    public void StartDialogue(DialogueData dialogue, System.Action onComplete = null)
    {
        if (dialogueUIManager != null)
        {
            isInDialogue = true;
            dialogueUIManager.ShowDialogue(dialogue, onComplete, this);
        }
    }

    // 结束对话
    public void EndDialogue()
    {
        isInDialogue = false;
    }

    // 获取主角信息
    public string GetPlayerName()
    {
        return playerName;
    }

    public Sprite GetPlayerPortrait()
    {
        return playerPortrait;
    }

    // 是否在对话中
    public bool IsInDialogue()
    {
        return isInDialogue;
    }
}