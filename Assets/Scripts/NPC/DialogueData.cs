using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string dialogueID;                       // 对话ID
    public string dialogueName;                     // 对话名称
    public List<DialogueNode> dialogueNodes = new List<DialogueNode>(); // 对话节点
    public bool repeatLastNode = false;              // 是否重复最后节点
}

[System.Serializable]
public class DialogueNode
{
    public string nodeID;                           // 节点ID
    public DialogueSpeaker speaker;                 // 说话者
    public string speakerName;                      // 说话者名称（覆盖默认）
    public Sprite speakerPortrait;                  // 说话者头像（覆盖默认）

    [TextArea(3, 5)]
    public string content;                          // 对话内容

    public float textSpeed = 0.05f;                 // 打字速度
    public AudioClip voiceClip;                     // 语音

    public List<DialogueChoice> choices = new List<DialogueChoice>(); // 选项
    public string nextNodeID;                       // 下一个节点ID（无选项时）
    public bool autoAdvance = false;                // 是否自动推进
    public float autoAdvanceDelay = 2f;              // 自动推进延迟
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;                       // 选项文本
    public string nextNodeID;                       // 选择后跳转的节点
    public DialogueAction action;                   // 选择后执行的动作
}

public enum DialogueSpeaker
{
    Player,     // 主角
    NPC,        // NPC
    Narrator    // 旁白
}

public enum DialogueAction
{
    None,           // 无动作
    EndDialogue,    // 结束对话
    GiveItem,       // 给予物品
    UnlockSkill,    // 解锁技能
    SetFlag,        // 设置标志
    StartQuest      // 开始任务
}