using UnityEngine;
using System.Collections.Generic;

public class DialogueCreator : MonoBehaviour
{
    [ContextMenu("创建示例多角色对话")]
    void CreateMultiCharDialogue()
    {
        // 创建对话数据
        DialogueData dialogue = ScriptableObject.CreateInstance<DialogueData>();
        dialogue.dialogueID = "first_meet";
        dialogue.dialogueName = "初次见面";

        dialogue.dialogueNodes = new List<DialogueNode>
        {
            // 节点1: NPC说话
            new DialogueNode
            {
                nodeID = "node_01",
                speaker = DialogueSpeaker.NPC,
                speakerName = "FlyingSprite",
                content = "…………",
                textSpeed = 0.01f,
                nextNodeID = "node_02"
            },

            new DialogueNode
            {
                nodeID = "node_02",
                speaker = DialogueSpeaker.NPC,
                speakerName = "FlyingSprite",
                content = "…………",
                textSpeed = 0.01f,
                nextNodeID = "node_03"
            },

            new DialogueNode
            {
                nodeID = "node_03",
                speaker = DialogueSpeaker.NPC,
                speakerName = "FlyingSprite",
                content = "未能找到原始数据……正在重启……",
                textSpeed = 0.01f,
                nextNodeID = "node_04"
            },
            
            new DialogueNode
            {
                nodeID = "node_04",
                speaker = DialogueSpeaker.NPC,
                speakerName = "FlyingSprite",
                content = "…………",
                textSpeed = 0.01f,
                nextNodeID = "node_05"
            },

            new DialogueNode
            {
                nodeID = "node_05",
                speaker = DialogueSpeaker.NPC,
                speakerName = "FlyingSprite",
                content = "—重启成功—",
                textSpeed = 0.05f,
                nextNodeID = "node_06"
            },

            new DialogueNode
            {
                nodeID = "node_06",
                speaker = DialogueSpeaker.NPC,
                speakerName = "FlyingSprite",
                content = "—你—好——",
                textSpeed = 0.01f,
                nextNodeID = "node_07"
            },

            new DialogueNode
            {
                nodeID = "node_07",
                speaker = DialogueSpeaker.NPC,
                speakerName = "FlyingSprite",
                content = "你好呀，我叫伊瑟，是你的游戏向导，接下来我会跟随你并协助你登上山顶。",
                textSpeed = 0.05f,
                nextNodeID = "node_08"
            },

        };

        // 保存
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.CreateAsset(dialogue, "Assets/Dialogues/VillageElderDialogue.asset");
        UnityEditor.AssetDatabase.SaveAssets();
#endif

        Debug.Log("创建多角色对话数据成功！");
    }
}