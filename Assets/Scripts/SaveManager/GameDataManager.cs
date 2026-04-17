using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager instance;
    private bool isloadingsave;

    // 当前游戏数据
    private SaveData currentGameData;

    // 当前游戏数据的公共访问属性
    public SaveData CurrentGameData
    {
        get { return currentGameData; }
        set
        {
            currentGameData = value;
            // 当设置新数据时，触发数据更新事件
            OnGameDataUpdated?.Invoke(currentGameData);
        }
    }

    // 是否正在加载游戏（用于避免重复加载）
    private bool isLoading = false;

    // 数据更新事件
    public System.Action<SaveData> OnGameDataUpdated;
    // 游戏开始新游戏事件
    public System.Action OnNewGameStarted;
    // 游戏加载完成事件
    public System.Action<SaveData> OnGameLoaded;

    [Header("新游戏初始位置")]
    public Vector2 defaultStartPosition = new Vector2(-4.5f, 9.15f);  // 默认起始位置
    public string defaultStartScene = "SampleScene";            // 默认起始场景

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // 取消订阅
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #region 游戏初始化

    // 创建新游戏
    public void NewGame(int saveSlot)
    {
        Debug.Log($"GameDataManager: 在存档槽 {saveSlot + 1} 创建新游戏");

        // 创建新的存档数据
        currentGameData = SaveData.CreateNewSave(saveSlot);

        // 设置初始场景（如果不是MENU，可以改为实际初始场景名）
        currentGameData.currentSceneName = defaultStartScene;
        currentGameData.playerPosition = defaultStartPosition;

        // 初始化血量
        currentGameData.playerHealth = 3;
        currentGameData.playerMaxHealth = 3;
        currentGameData.HealthCollection = 0;

        // 初始化物品收集字典
        if (currentGameData.Collections == null)
            currentGameData.Collections = new Dictionary<string, bool>();

        // 初始化NPC进度字典
        if (currentGameData.npcProgress == null)
            currentGameData.npcProgress = new Dictionary<string, NPCProgress>();

        // 初始化技能字典
        if (currentGameData.unlockedSkills == null)
            currentGameData.unlockedSkills = new Dictionary<string, bool>();

        if (currentGameData.unlockedTrans == null)
            currentGameData.unlockedTrans = new Dictionary<string, bool>();

        // 触发事件
        OnNewGameStarted?.Invoke();
        OnGameDataUpdated?.Invoke(currentGameData);

        Debug.Log($"新游戏创建成功，起始场景：{currentGameData.currentSceneName}");
    }

    // 加载游戏数据
    public void LoadGameData(SaveData loadedData)
    {
        if (loadedData == null)
        {
            Debug.LogError("GameDataManager: 尝试加载空数据");
            return;
        }

        isLoading = true;

        // 复制加载的数据到当前游戏数据
        currentGameData = loadedData;
        currentGameData.hasData = true;

        Debug.Log($"游戏数据加载成功：槽位 {loadedData.saveSlot + 1}, 场景：{loadedData.currentSceneName}");

        // 触发事件
        OnGameLoaded?.Invoke(loadedData);
        OnGameDataUpdated?.Invoke(currentGameData);

        isLoading = false;

        isloadingsave = true;
    }

    #endregion

    #region 数据更新方法

    // 更新玩家位置
    public void UpdatePlayerPosition(Vector2 position)
    {
        if (currentGameData != null)
        {
            currentGameData.playerPosition = position;
        }
    }

    // 更新玩家血量
    public void UpdatePlayerHealth(int currentHealth, int maxHealth)
    {
        if (currentGameData != null)
        {
            currentGameData.playerHealth = currentHealth;
            currentGameData.playerMaxHealth = maxHealth;
        }
    }

    // 更新当前场景
    public void UpdateCurrentScene()
    {
        if (currentGameData != null)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            currentGameData.currentSceneName = activeScene.name;

            Debug.Log($"场景更新为：{activeScene.name}");
        }
    }

    // 更新游戏时间
    public void UpdatePlayTime(float deltaTime)
    {
        if (currentGameData != null)
        {
            currentGameData.playTime += deltaTime;
        }
    }

    // 更新物品收集状态
    public void UpdateCollection(string collectionID, bool collected)
    {
        if (currentGameData != null)
        {
            if (currentGameData.Collections.ContainsKey(collectionID))
            {
                currentGameData.Collections[collectionID] = collected;
            }
            else
            {
                currentGameData.Collections.Add(collectionID, collected);
            }

            Debug.Log($"物品收集更新：{collectionID} = {collected}");
            OnGameDataUpdated?.Invoke(currentGameData);
        }
    }

    // 更新生命收集物数量
    public void UpdateHealthCollection(int amount)
    {
        if (currentGameData != null)
        {
            currentGameData.HealthCollection = amount;
            OnGameDataUpdated?.Invoke(currentGameData);
        }
    }

    // 增加生命收集物
    public void AddHealthCollection(int amount = 1)
    {
        if (currentGameData != null)
        {
            currentGameData.HealthCollection += amount;
            OnGameDataUpdated?.Invoke(currentGameData);
        }
    }

    // 更新NPC进度
    public void UpdateNPCProgress(string npcID, NPCProgress progress)
    {
        if (currentGameData != null)
        {
            if (currentGameData.npcProgress.ContainsKey(npcID))
            {
                currentGameData.npcProgress[npcID] = progress;
            }
            else
            {
                currentGameData.npcProgress.Add(npcID, progress);
            }

            Debug.Log($"NPC进度更新：{npcID}");
            OnGameDataUpdated?.Invoke(currentGameData);
        }
    }

    // 更新NPC好感度
    public void UpdateNPCRelationship(string npcID, string relationshipKey, int value)
    {
        if (currentGameData != null)
        {
            if (!currentGameData.npcProgress.ContainsKey(npcID))
            {
                currentGameData.npcProgress[npcID] = new NPCProgress { npcID = npcID };
            }

            NPCProgress npc = currentGameData.npcProgress[npcID];

            if (npc.relationshipValue.ContainsKey(relationshipKey))
            {
                npc.relationshipValue[relationshipKey] = value;
            }
            else
            {
                npc.relationshipValue.Add(relationshipKey, value);
            }

            OnGameDataUpdated?.Invoke(currentGameData);
        }
    }

    // 完成NPC任务
    public void CompleteNPCQuest(string npcID, string questID)
    {
        if (currentGameData != null)
        {
            if (!currentGameData.npcProgress.ContainsKey(npcID))
            {
                currentGameData.npcProgress[npcID] = new NPCProgress { npcID = npcID };
            }

            NPCProgress npc = currentGameData.npcProgress[npcID];

            if (!npc.completedQuests.Contains(questID))
            {
                npc.completedQuests.Add(questID);
                Debug.Log($"任务完成：{questID}");
            }

            OnGameDataUpdated?.Invoke(currentGameData);
        }
    }

    // 解锁技能
    public void UnlockSkill(string skillID)
    {
        if (currentGameData != null)
        {
            if (!currentGameData.unlockedSkills.ContainsKey(skillID))
            {
                currentGameData.unlockedSkills.Add(skillID, true);
                Debug.Log($"技能解锁：{skillID}");
            }
            else
            {
                currentGameData.unlockedSkills[skillID] = true;
            }

            OnGameDataUpdated?.Invoke(currentGameData);
        }
    }

    // 解锁变形
    public void UnlockTransformation(string transID)
    {
        if (currentGameData != null)
        {
            if (!currentGameData.unlockedTrans.ContainsKey(transID))
            {
                currentGameData.unlockedTrans.Add(transID, true);
                Debug.Log($"变形解锁：{transID}");
            }
            else
            {
                currentGameData.unlockedTrans[transID] = true;
            }

            OnGameDataUpdated?.Invoke(currentGameData);
        }
    }

    // 检查物品是否已收集
    public bool IsCollectionCollected(string collectionID)
    {
        if (currentGameData?.Collections == null)
            return false;

        return currentGameData.Collections.ContainsKey(collectionID) &&
               currentGameData.Collections[collectionID];
    }

    // 检查技能是否已解锁
    public bool IsSkillUnlocked(string skillID)
    {
        if (currentGameData?.unlockedSkills == null)
            return false;

        return currentGameData.unlockedSkills.ContainsKey(skillID) &&
               currentGameData.unlockedSkills[skillID];
    }

    // 检查变形是否已解锁
    public bool IsTransformationUnlocked(string transID)
    {
        if (currentGameData?.unlockedTrans == null)
            return false;

        return currentGameData.unlockedTrans.ContainsKey(transID) &&
               currentGameData.unlockedTrans[transID];
    }

    #endregion

    #region 场景管理

    // 场景加载完成时的处理
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (currentGameData != null && !isLoading)
        {
            Debug.Log($"场景加载完成，当前场景：{scene.name}");

            // 如果是加载游戏后第一次进入场景，应用位置数据
            if (isloadingsave == true)
            {
                StartCoroutine(ApplyPlayerPosition());
            }
        }
    }

    private IEnumerator ApplyPlayerPosition()
    {
        // 等待一帧，确保玩家对象已经生成
        yield return null;

        // 再等待一帧，确保所有组件都初始化完成
        yield return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && currentGameData != null)
        {
            // 设置玩家位置
            player.transform.position = currentGameData.playerPosition;
            Debug.Log($"玩家位置已设置为存档位置：{currentGameData.playerPosition}");

            // 设置玩家血量
            /*
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.currentHealth = currentGameData.playerHealth;
                health.maxHealth = currentGameData.playerMaxHealth;
                Debug.Log($"玩家血量已设置为：{currentGameData.playerHealth}/{currentGameData.playerMaxHealth}");
            }
            */
        }
          
        else
        {
            Debug.LogWarning($"找不到玩家对象或没有游戏数据 - Player: {player != null}, GameData: {currentGameData != null}");
        }

        isloadingsave = false;
    }

    // 保存当前数据到存档管理器
    public void SaveCurrentGame()
    {
        if (currentGameData == null)
        {
            Debug.LogError("没有当前游戏数据可以保存");
            return;
        }

        // 更新场景信息
        UpdateCurrentScene();

        // 调用SaveManager保存
        bool success = SaveManager.instance.SaveGame(currentGameData.saveSlot, currentGameData);

        if (success)
        {
            Debug.Log($"游戏保存成功：槽位 {currentGameData.saveSlot + 1}");
        }
        else
        {
            Debug.LogError("游戏保存失败");
        }
    }

    #endregion

    #region 数据查询方法

    // 获取NPC进度
    public NPCProgress GetNPCProgress(string npcID)
    {
        if (currentGameData?.npcProgress == null)
            return null;

        return currentGameData.npcProgress.ContainsKey(npcID) ?
               currentGameData.npcProgress[npcID] : null;
    }

    // 获取NPC好感度
    public int GetNPCRelationship(string npcID, string relationshipKey)
    {
        NPCProgress npc = GetNPCProgress(npcID);

        if (npc?.relationshipValue == null)
            return 0;

        return npc.relationshipValue.ContainsKey(relationshipKey) ?
               npc.relationshipValue[relationshipKey] : 0;
    }

    // 检查NPC任务是否完成
    public bool IsNPCQuestCompleted(string npcID, string questID)
    {
        NPCProgress npc = GetNPCProgress(npcID);

        if (npc?.completedQuests == null)
            return false;

        return npc.completedQuests.Contains(questID);
    }

    // 获取存档预览信息（存档编号 | 场景名 | 最后存档时间）
    public string GetSavePreviewInfo()
    {
        if (currentGameData == null)
            return "无存档数据";

        string sceneName = string.IsNullOrEmpty(currentGameData.currentSceneName) ? "未知场景" : currentGameData.currentSceneName;
        return $"存档{currentGameData.saveSlot + 1} | {sceneName} | {currentGameData.saveTime:MM-dd HH:mm}";
    }

    // 检查是否有当前游戏数据
    public bool HasCurrentGame()
    {
        return currentGameData != null && currentGameData.hasData;
    }

    #endregion

    #region 调试方法

    // 打印当前游戏数据（用于调试）
    public void PrintCurrentGameData()
    {
        if (currentGameData == null)
        {
            Debug.Log("当前没有游戏数据");
            return;
        }

        string info = $"=== 当前游戏数据 ===\n" +
                     $"存档槽位：{currentGameData.saveSlot + 1}\n" +
                     $"存档时间：{currentGameData.saveTime}\n" +
                     $"玩家血量：{currentGameData.playerHealth}/{currentGameData.playerMaxHealth}\n" +
                     $"当前位置：{currentGameData.playerPosition}\n" +
                     $"当前场景：{currentGameData.currentSceneName}\n" +
                     $"游戏时间：{FormatPlayTime(currentGameData.playTime)}\n" +
                     $"生命收集物：{currentGameData.HealthCollection}\n" +
                     $"物品收集数量：{currentGameData.Collections?.Count ?? 0}\n" +
                     $"NPC进度数量：{currentGameData.npcProgress?.Count ?? 0}\n" +
                     $"已解锁技能数量：{currentGameData.unlockedSkills?.Count ?? 0}\n" +
                     $"已解锁变形数量：{currentGameData.unlockedTrans?.Count ?? 0}";

        Debug.Log(info);
    }

    private string FormatPlayTime(float totalSeconds)
    {
        int hours = Mathf.FloorToInt(totalSeconds / 3600);
        int minutes = Mathf.FloorToInt((totalSeconds % 3600) / 60);

        if (hours > 0)
            return $"{hours}小时{minutes}分钟";
        else
            return $"{minutes}分钟";
    }

    #endregion
}
