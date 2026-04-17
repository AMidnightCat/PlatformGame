using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DialogueUIManager : MonoBehaviour
{
    public static DialogueUIManager Instance;

    [Header("UI组件")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public Image speakerPortraitImage;
    public TextMeshProUGUI dialogueText;
    public GameObject continuePrompt;
    public GameObject choicesPanel;
    public GameObject choiceButtonPrefab;

    [Header("打字机设置")]
    public float defaultTextSpeed = 0.05f;
    public AudioClip typingSound;
    public AudioSource audioSource;

    [Header("动画设置")]
    public Animator panelAnimator;
    public string showAnimationTrigger = "Show";
    public string hideAnimationTrigger = "Hide";

    private DialogueData currentDialogue;
    private Dictionary<string, DialogueNode> nodeMap;
    private DialogueNode currentNode;
    private Action onComplete;
    private PlayerDialogueController playerController;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private bool canAdvance = false;
    private bool isWaitingForChoice = false;

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
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (continuePrompt != null)
            continuePrompt.SetActive(false);
        if (choicesPanel != null)
            choicesPanel.SetActive(false);
    }

    void Update()
    {
        // 检测玩家输入推进对话
        if (dialoguePanel.activeSelf && canAdvance && !isWaitingForChoice)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F))
            {
                AdvanceToNextNode();
            }
        }
    }

    /// <summary>
    /// 显示对话
    /// </summary>
    public void ShowDialogue(DialogueData dialogue, Action onCompleteCallback, PlayerDialogueController playerControllerRef)
    {
        if (dialogue == null || dialogue.dialogueNodes.Count == 0)
        {
            Debug.LogError("对话数据为空");
            onCompleteCallback?.Invoke();
            return;
        }

        currentDialogue = dialogue;
        onComplete = onCompleteCallback;
        playerController = playerControllerRef;

        // 构建节点映射
        nodeMap = new Dictionary<string, DialogueNode>();
        foreach (var node in dialogue.dialogueNodes)
        {
            if (!string.IsNullOrEmpty(node.nodeID))
            {
                nodeMap[node.nodeID] = node;
            }
        }

        // 从第一个节点开始
        if (dialogue.dialogueNodes.Count > 0)
        {
            ShowNode(dialogue.dialogueNodes[0]);
        }

        // 显示面板
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            if (panelAnimator != null && !string.IsNullOrEmpty(showAnimationTrigger))
            {
                panelAnimator.SetTrigger(showAnimationTrigger);
            }
        }
    }

    /// <summary>
    /// 显示对话节点
    /// </summary>
    void ShowNode(DialogueNode node)
    {
        currentNode = node;
        isWaitingForChoice = false;

        // 设置说话者信息
        string speakerName = GetSpeakerName(node);
        Sprite speakerPortrait = GetSpeakerPortrait(node);

        if (speakerNameText != null)
            speakerNameText.text = speakerName;
        if (speakerPortraitImage != null)
            speakerPortraitImage.sprite = speakerPortrait;

        // 隐藏选项面板
        if (choicesPanel != null)
            choicesPanel.SetActive(false);

        // 开始打字效果
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(node));

        // 播放语音
        if (node.voiceClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(node.voiceClip);
        }
    }

    /// <summary>
    /// 获取说话者名称
    /// </summary>
    string GetSpeakerName(DialogueNode node)
    {
        if (!string.IsNullOrEmpty(node.speakerName))
            return node.speakerName;

        switch (node.speaker)
        {
            case DialogueSpeaker.Player:
                return playerController != null ? playerController.GetPlayerName() : "冒险者";
            case DialogueSpeaker.NPC:
                return "???"; // 实际应该从NPC获取
            case DialogueSpeaker.Narrator:
                return "旁白";
            default:
                return "???";
        }
    }

    /// <summary>
    /// 获取说话者头像
    /// </summary>
    Sprite GetSpeakerPortrait(DialogueNode node)
    {
        if (node.speakerPortrait != null)
            return node.speakerPortrait;

        switch (node.speaker)
        {
            case DialogueSpeaker.Player:
                return playerController != null ? playerController.GetPlayerPortrait() : null;
            default:
                return null;
        }
    }

    /// <summary>
    /// 打字机效果
    /// </summary>
    IEnumerator TypeText(DialogueNode node)
    {
        isTyping = true;
        canAdvance = false;

        if (continuePrompt != null)
            continuePrompt.SetActive(false);

        dialogueText.text = "";
        float textSpeed = node.textSpeed > 0 ? node.textSpeed : defaultTextSpeed;

        foreach (char c in node.content.ToCharArray())
        {
            dialogueText.text += c;

            if (typingSound != null && audioSource != null && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(typingSound);
            }

            yield return new WaitForSecondsRealtime(textSpeed);
        }

        isTyping = false;

        // 检查是否有选项
        if (node.choices != null && node.choices.Count > 0)
        {
            ShowChoices(node.choices);
            canAdvance = false;
            isWaitingForChoice = true;
        }
        else if (node.autoAdvance)
        {
            yield return new WaitForSecondsRealtime(node.autoAdvanceDelay);
            AdvanceToNextNode();
        }
        else
        {
            canAdvance = true;
            if (continuePrompt != null)
                continuePrompt.SetActive(true);
        }
    }

    /// <summary>
    /// 显示选项
    /// </summary>
    void ShowChoices(List<DialogueChoice> choices)
    {
        if (choicesPanel == null || choiceButtonPrefab == null) return;

        // 清除现有选项
        foreach (Transform child in choicesPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // 创建选项按钮
        foreach (var choice in choices)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choicesPanel.transform);
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choice.choiceText;
            }

            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                DialogueChoice choiceCopy = choice;
                button.onClick.AddListener(() => OnChoiceSelected(choiceCopy));
            }
        }

        choicesPanel.SetActive(true);
    }

    /// <summary>
    /// 选项被选择
    /// </summary>
    void OnChoiceSelected(DialogueChoice choice)
    {
        // 执行动作
        ExecuteAction(choice.action);

        // 跳转到下一个节点
        if (!string.IsNullOrEmpty(choice.nextNodeID) && nodeMap.ContainsKey(choice.nextNodeID))
        {
            ShowNode(nodeMap[choice.nextNodeID]);
        }
        else
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// 执行对话动作
    /// </summary>
    void ExecuteAction(DialogueAction action)
    {
        switch (action)
        {
            case DialogueAction.EndDialogue:
                EndDialogue();
                break;
            case DialogueAction.GiveItem:
                // TODO: 实现给予物品逻辑
                Debug.Log("获得物品");
                break;
            case DialogueAction.UnlockSkill:
                // TODO: 实现解锁技能逻辑
                Debug.Log("解锁技能");
                break;
            case DialogueAction.SetFlag:
                // TODO: 实现设置标志逻辑
                Debug.Log("设置剧情标志");
                break;
            case DialogueAction.StartQuest:
                // TODO: 实现开始任务逻辑
                Debug.Log("开始任务");
                break;
        }
    }

    /// <summary>
    /// 推进到下一个节点
    /// </summary>
    void AdvanceToNextNode()
    {
        if (!canAdvance || isTyping || isWaitingForChoice) return;

        if (!string.IsNullOrEmpty(currentNode.nextNodeID) && nodeMap.ContainsKey(currentNode.nextNodeID))
        {
            ShowNode(nodeMap[currentNode.nextNodeID]);
        }
        else
        {
            EndDialogue();
        }
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    void EndDialogue()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // 播放隐藏动画
        if (panelAnimator != null && !string.IsNullOrEmpty(hideAnimationTrigger))
        {
            panelAnimator.SetTrigger(hideAnimationTrigger);
            StartCoroutine(HideAfterAnimation());
        }
        else
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        // 回调
        onComplete?.Invoke();
        if (playerController != null)
            playerController.EndDialogue();

        // 重置状态
        isTyping = false;
        canAdvance = false;
        isWaitingForChoice = false;
        if (continuePrompt != null)
            continuePrompt.SetActive(false);
        if (choicesPanel != null)
            choicesPanel.SetActive(false);
    }

    IEnumerator HideAfterAnimation()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    /// <summary>
    /// 隐藏对话
    /// </summary>
    public void HideDialogue()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        isTyping = false;
        canAdvance = false;
        isWaitingForChoice = false;

        if (playerController != null)
            playerController.EndDialogue();
    }

    /// <summary>
    /// 是否在对话中
    /// </summary>
    public bool IsInDialogue()
    {
        return dialoguePanel != null && dialoguePanel.activeSelf;
    }
}