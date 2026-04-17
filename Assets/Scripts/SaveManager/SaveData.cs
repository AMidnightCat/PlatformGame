using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Windows;

// 可序列化的存档数据类
[System.Serializable]
public class SaveData
{
    // 存档元数据
    public string saveFileName;        // 存档文件名
    public DateTime saveTime;           // 存档时间
    public int saveSlot;                // 存档槽位
    public bool hasData;                // 是否有数据（是否为空存档）

    // 玩家基础数据
    public Vector2 playerPosition;      // 玩家位置
    public int playerHealth;          // 玩家血量
    public int playerMaxHealth;       // 最大血量

    // 物品收集数据
    public Dictionary<string, bool> Collections = new Dictionary<string, bool>();
    public int HealthCollection;

    // NPC进度数据
    public Dictionary<string, NPCProgress> npcProgress = new Dictionary<string, NPCProgress>();

    // 技能解锁数据
    public Dictionary<string, bool> unlockedSkills = new Dictionary<string, bool>();
    public Dictionary<string, bool> unlockedTrans = new Dictionary<string, bool>();

    // 游戏进度数据
    public string currentSceneName;      // 场景名称
    public float playTime;               // 游戏时间

    // 构造函数
    public SaveData()
    {
        saveTime = DateTime.Now;
        hasData = false;
        playTime = 0f;
    }

    // 创建新存档
    public static SaveData CreateNewSave(int slot)
    {
        return new SaveData
        {
            saveSlot = slot,
            hasData = true,
            saveTime = DateTime.Now,
            playerHealth = 3,
            playerMaxHealth = 3,
            playTime = 0f,
            currentSceneName = "MENU"
        };
    }

}

// NPC进度类
[System.Serializable]
public class NPCProgress
{
    public string npcID;
    public int dialogueIndex;           // 对话进度
    public bool questCompleted;         // 任务完成状态
    public List<string> completedQuests = new List<string>();  // 已完成任务列表
    public Dictionary<string, int> relationshipValue = new Dictionary<string, int>(); // 好感度
}
