using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [Header("NPC信息")]
    public string npcID;                            // NPC唯一ID
    public string npcName = "NPC";                  // NPC显示名称
    public Sprite npcPortrait;                      // NPC头像
    
    [Header("对话数据")]
    public DialogueData[] dialogueProgress;         // 不同进度的对话数据（按顺序）
    
    [Header("交互设置")]
    public KeyCode interactKey = KeyCode.E;         // 交互按键
    public float interactionRadius = 2f;            // 交互范围
    public GameObject interactionPrompt;            // 交互提示UI
    
    [Header("视觉设置")]
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(1f, 0.9f, 0.7f);
    
    [Header("启用飞行精灵设置")]
    public string flyingSpriteName = "FlyingSprite"; // 要启用的飞行精灵名称
    public bool enableFlyingSpriteOnComplete = true; // 对话完成后是否启用飞行精灵
    
    private GameDataManager gameDataManager;
    private PlayerDialogueController playerDialogue;
    private SpriteRenderer spriteRenderer;
    private int currentProgress = 0;
    private bool playerInRange = false;
    private bool isInDialogue = false;
    
    // 对话完成事件
    public System.Action OnDialogueComplete;
    
    void Start()
    {
        gameDataManager = GameDataManager.instance;
        playerDialogue = PlayerDialogueController.Instance;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
        
        // 从存档加载NPC进度
        LoadNPCProgress();
    }
    
    void Update()
    {
        // 检测玩家在范围内且按下交互键
        if (playerInRange && Input.GetKeyDown(interactKey) && playerDialogue != null && !playerDialogue.IsInDialogue() && !isInDialogue)
        {
            StartDialogue();
        }
        
        // 高亮效果
        if (spriteRenderer != null)
        {
            spriteRenderer.color = playerInRange && !isInDialogue ? highlightColor : normalColor;
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (interactionPrompt != null)
                interactionPrompt.SetActive(true);
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }
    }
    
    void StartDialogue()
    {
        if (dialogueProgress == null || dialogueProgress.Length == 0)
        {
            Debug.LogWarning($"NPC {npcName} 没有对话数据");
            return;
        }
        
        // 确保进度不超出范围
        if (currentProgress >= dialogueProgress.Length)
        {
            if (dialogueProgress[currentProgress - 1].repeatLastNode)
            {
                isInDialogue = true;
                playerDialogue.StartDialogue(dialogueProgress[currentProgress - 1], OnDialogueCompleteCallback);
                return;
            }
            return;
        }
        
        isInDialogue = true;
        playerDialogue.StartDialogue(dialogueProgress[currentProgress], OnDialogueCompleteCallback);
    }
    
    void OnDialogueCompleteCallback()
    {
        isInDialogue = false;
        
        // 推进剧情进度
        if (currentProgress < dialogueProgress.Length)
        {
            currentProgress++;
            SaveNPCProgress();
        }
        
        Debug.Log($"{npcName} 对话完成，当前进度: {currentProgress}/{dialogueProgress.Length}");
        
        // 触发对话完成事件
        OnDialogueComplete?.Invoke();
        
        // 检查是否需要启用飞行精灵
        if (enableFlyingSpriteOnComplete)
        {
            CheckAndEnableFlyingSprite();
        }
    }
    
    //检查并启用飞行精灵
    void CheckAndEnableFlyingSprite()
    {
        // 方法1：通过名称查找
        GameObject flyingSpriteObj = GameObject.Find(flyingSpriteName);
        
        // 方法2：如果没找到，通过类型查找
        if (flyingSpriteObj == null)
        {
            FlyingSprite flyingSprite = FindObjectOfType<FlyingSprite>();
            if (flyingSprite != null)
            {
                flyingSpriteObj = flyingSprite.gameObject;
            }
        }
        
        if (flyingSpriteObj != null)
        {
            FlyingSprite flyingSprite = flyingSpriteObj.GetComponent<FlyingSprite>();
            if (flyingSprite != null)
            {
                flyingSprite.EnableSprite();
                Debug.Log($"对话完成，飞行精灵 {flyingSpriteName} 已启用！");
            }
        }
        else
        {
            Debug.LogWarning($"未找到飞行精灵: {flyingSpriteName}");
        }
    }
    
    /// <summary>
    /// 加载NPC进度
    /// </summary>
    void LoadNPCProgress()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            NPCProgress progress = gameDataManager.GetNPCProgress(npcID);
            if (progress != null)
            {
                currentProgress = progress.dialogueIndex;
                Debug.Log($"加载 {npcName} 进度: {currentProgress}");
            }
        }
    }
    
    /// <summary>
    /// 保存NPC进度
    /// </summary>
    void SaveNPCProgress()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            NPCProgress progress = new NPCProgress
            {
                npcID = npcID,
                dialogueIndex = currentProgress,
                questCompleted = false,
                completedQuests = new List<string>(),
                relationshipValue = new Dictionary<string, int>()
            };
            gameDataManager.UpdateNPCProgress(npcID, progress);
            Debug.Log($"保存 {npcName} 进度: {currentProgress}");
        }
    }
    
    /// <summary>
    /// 手动设置进度（用于调试或特殊情况）
    /// </summary>
    public void SetProgress(int progress)
    {
        currentProgress = Mathf.Clamp(progress, 0, dialogueProgress.Length);
        SaveNPCProgress();
    }
    
    /// <summary>
    /// 获取当前进度
    /// </summary>
    public int GetProgress()
    {
        return currentProgress;
    }
    
    /// <summary>
    /// 检查是否已完成所有对话
    /// </summary>
    public bool IsCompleted()
    {
        return currentProgress >= dialogueProgress.Length;
    }
    
    /// <summary>
    /// 重置NPC进度
    /// </summary>
    public void ResetProgress()
    {
        currentProgress = 0;
        SaveNPCProgress();
        Debug.Log($"重置 {npcName} 进度");
    }
    
    /// <summary>
    /// 手动触发对话（用于外部调用）
    /// </summary>
    public void TriggerDialogue()
    {
        if (!isInDialogue && playerDialogue != null && !playerDialogue.IsInDialogue())
        {
            StartDialogue();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // 绘制交互范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}