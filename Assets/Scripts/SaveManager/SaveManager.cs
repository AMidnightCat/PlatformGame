using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    private string savePath;
    private const string SAVE_EXTENSION = ".json";
    public const int maxSaveSlots = 15;
    public SaveData[] saveSlots = new SaveData[maxSaveSlots];

    // 当前选中的存档槽
    public int currentSelectedSlot = -1;

    // 存档槽信息类（用于UI显示）
    [System.Serializable]
    public class SlotInfo
    {
        public int slotIndex;
        public bool hasData;
        public DateTime lastSaveTime;
        public string previewInfo;
        public string sceneName;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Application.persistentDataPath + "/saves/";
            InitializeSaveDirectory();
            LoadAllSaveMetadata();  // 加载所有存档元数据
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 初始化存档目录
    void InitializeSaveDirectory()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
    }

    // 加载所有存档的元数据
    void LoadAllSaveMetadata()
    {
        // 初始化所有存档槽位
        for (int i = 0; i < maxSaveSlots; i++)
        {
            saveSlots[i] = new SaveData { saveSlot = i, hasData = false };
        }

        // 查找所有存档文件
        string[] files = Directory.GetFiles(savePath, $"*{SAVE_EXTENSION}");

        foreach (string file in files)
        {
            try
            {
                string jsonString = File.ReadAllText(file);
                SaveData data = JsonConvert.DeserializeObject<SaveData>(jsonString,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                // 根据存档槽位放入对应位置
                if (data != null && data.saveSlot >= 0 && data.saveSlot < maxSaveSlots)
                {
                    data.hasData = true;
                    saveSlots[data.saveSlot] = data;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"读取存档文件失败：{file}，错误：{e.Message}");
            }
        }
    }

    #region 存档操作

    // 保存游戏（覆盖）
    public bool SaveGame(int slotIndex, SaveData data)
    {
        try
        {
            if (slotIndex < 0 || slotIndex >= maxSaveSlots)
            {
                Debug.LogError($"无效的存档槽位：{slotIndex}");
                return false;
            }

            // 创建一个新的SaveData副本，避免影响内存中的其他引用
            SaveData saveCopy = new SaveData();

            // 复制所有数据
            saveCopy.saveSlot = slotIndex;
            saveCopy.saveTime = DateTime.Now;  // 只更新当前保存的时间
            saveCopy.hasData = true;
            saveCopy.saveFileName = GetSaveFileName(slotIndex);

            // 复制玩家数据
            saveCopy.playerPosition = data.playerPosition;
            saveCopy.playerHealth = data.playerHealth;
            saveCopy.playerMaxHealth = data.playerMaxHealth;
            saveCopy.HealthCollection = data.HealthCollection;

            // 复制场景数据
            saveCopy.currentSceneName = data.currentSceneName;

            // 复制游戏进度数据
            saveCopy.playTime = data.playTime;

            // 深拷贝字典数据
            saveCopy.Collections = new Dictionary<string, bool>(data.Collections);
            saveCopy.npcProgress = new Dictionary<string, NPCProgress>();

            // 深拷贝NPCProgress
            foreach (var kvp in data.npcProgress)
            {
                NPCProgress npcCopy = new NPCProgress
                {
                    npcID = kvp.Value.npcID,
                    dialogueIndex = kvp.Value.dialogueIndex,
                    questCompleted = kvp.Value.questCompleted,
                    completedQuests = new List<string>(kvp.Value.completedQuests),
                    relationshipValue = new Dictionary<string, int>(kvp.Value.relationshipValue)
                };
                saveCopy.npcProgress.Add(kvp.Key, npcCopy);
            }

            // 深拷贝技能字典
            saveCopy.unlockedSkills = new Dictionary<string, bool>(data.unlockedSkills);
            saveCopy.unlockedTrans = new Dictionary<string, bool>(data.unlockedTrans);

            string filePath = GetSavePath(slotIndex);

            string jsonString = JsonConvert.SerializeObject(saveCopy, Formatting.Indented,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

            File.WriteAllText(filePath, jsonString);

            // 只更新当前槽位的数据
            saveSlots[slotIndex] = saveCopy;

            Debug.Log($"游戏保存成功到槽位 {slotIndex + 1}，存档时间：{saveCopy.saveTime}");
            OnSaveUpdated?.Invoke(slotIndex, saveCopy);

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存失败：{e.Message}");
            return false;
        }
    }


    // 加载存档
    public SaveData LoadGame(int slotIndex)
    {
        try
        {
            // 参数验证
            if (slotIndex < 0 || slotIndex >= maxSaveSlots)
            {
                Debug.LogError($"无效的存档槽位：{slotIndex}");
                return null;
            }

            string filePath = GetSavePath(slotIndex);

            if (File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);
                SaveData data = JsonConvert.DeserializeObject<SaveData>(jsonString,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                if (data != null)
                {
                    data.hasData = true;
                    saveSlots[slotIndex] = data;
                    Debug.Log($"从槽位 {slotIndex + 1} 加载成功");

                    OnSaveLoaded?.Invoke(slotIndex, data);
                    return data;
                }
            }

            Debug.LogWarning($"槽位 {slotIndex + 1} 没有存档数据");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载失败：{e.Message}");
            return null;
        }
    }


    // 删除存档
    public bool DeleteSave(int slotIndex)
    {
        try
        {
            if (slotIndex < 0 || slotIndex >= maxSaveSlots)
            {
                Debug.LogError($"无效的存档槽位：{slotIndex}");
                return false;
            }

            string filePath = GetSavePath(slotIndex);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);

                // 重置内存中的存档数据
                saveSlots[slotIndex] = new SaveData { saveSlot = slotIndex, hasData = false };

                Debug.Log($"删除槽位 {slotIndex + 1} 的存档成功");

                OnSaveDeleted?.Invoke(slotIndex);
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"删除失败：{e.Message}");
        }

        return false;
    }

    #endregion

    #region 辅助方法

    // 获取存档文件名（固定格式）
    string GetSaveFileName(int slotIndex)
    {
        return $"Save_Slot_{slotIndex + 1}{SAVE_EXTENSION}";
    }

    // 获取存档路径
    string GetSavePath(int slotIndex)
    {
        return Path.Combine(savePath, GetSaveFileName(slotIndex));
    }

    // 检查指定槽位是否有存档
    public bool IsSlotOccupied(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSaveSlots)
            return false;

        return saveSlots[slotIndex] != null && saveSlots[slotIndex].hasData;
    }

    // 获取所有存档槽位信息
    public List<SlotInfo> GetAllSlotInfo()
    {
        List<SlotInfo> slotInfos = new List<SlotInfo>();

        for (int i = 0; i < maxSaveSlots; i++)
        {
            SlotInfo info = new SlotInfo
            {
                slotIndex = i,
                hasData = saveSlots[i] != null && saveSlots[i].hasData
            };

            if (info.hasData && saveSlots[i] != null)
            {
                info.lastSaveTime = saveSlots[i].saveTime;
                info.sceneName = string.IsNullOrEmpty(saveSlots[i].currentSceneName) ? "未知场景" : saveSlots[i].currentSceneName;
                
                // 预览信息：存档编号 | 场景名 | 最后存档时间
                info.previewInfo = $"存档{i + 1} | {info.sceneName} | {saveSlots[i].saveTime:yyyy-MM-dd HH:mm}";
            }

            slotInfos.Add(info);
        }

        return slotInfos;
    }

    // 获取指定槽位的存档数据
    public SaveData GetSlotData(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < maxSaveSlots)
        {
            return saveSlots[slotIndex];
        }
        return null;
    }

    #endregion

    #region 事件系统

    public System.Action<int, SaveData> OnSaveUpdated;
    public System.Action<int, SaveData> OnSaveLoaded;
    public System.Action<int> OnSaveDeleted;

    #endregion

}
