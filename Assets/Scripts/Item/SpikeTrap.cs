using System.Collections;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("伤害设置")]
    [SerializeField] private int damageAmount = 1;           // 伤害值
    [SerializeField] private float damageCooldown = 1f;      // 伤害冷却时间（避免连续触发）

    [Header("画面效果")]
    [SerializeField] private float fadeOutDuration = 0.5f;   // 渐黑持续时间
    [SerializeField] private float fadeInDuration = 0.5f;    // 恢复画面持续时间
    [SerializeField] private Color fadeColor = Color.black;  // 渐黑颜色
    [SerializeField] private int fadeSortingOrder = 100;     // 遮罩的排序层级

    [Header("延迟设置")]
    [SerializeField] private float delayBeforeTeleport = 0.2f;   // 扣血后等待多久传送
    [SerializeField] private float delayBeforeFadeIn = 0.1f;     // 传送后等待多久开始恢复画面

    private HealthManager healthManager;
    private TestDragonControl playerController;
    private Transform playerTransform;
    private Rigidbody2D playerRigidbody;
    private GameObject fadePanel;           // 渐隐遮罩对象
    private float lastDamageTime = -999f;   // 上次受伤时间
    private bool isProcessingDamage = false; // 是否正在处理伤害流程

    // 保存原始状态
    private bool originalIsKinematic;
    private float originalGravityScale;

    void Start()
    {
        // 查找玩家相关组件
        FindPlayer();

        // 创建渐隐遮罩
        CreateFadePanel();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 检查是否为玩家
        if (other.CompareTag("Player") || other.GetComponent<TestDragonControl>() != null)
        {
            // 如果玩家为空，重新查找
            if (playerTransform == null)
            {
                FindPlayer();
            }

            // 检查伤害冷却和是否正在处理伤害
            if (Time.time >= lastDamageTime + damageCooldown && !isProcessingDamage)
            {
                // 处理伤害和传送
                StartCoroutine(HandleDamageAndTeleport());
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 持续接触时的处理（如果需要）
        if (other.CompareTag("Player") && !isProcessingDamage)
        {
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                StartCoroutine(HandleDamageAndTeleport());
            }
        }
    }

    private void FindPlayer()
    {
        // 查找玩家对象
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = FindObjectOfType<TestDragonControl>()?.gameObject;
        }

        if (player != null)
        {
            playerTransform = player.transform;
            healthManager = player.GetComponent<HealthManager>();
            playerController = player.GetComponent<TestDragonControl>();
            playerRigidbody = player.GetComponent<Rigidbody2D>();
        }
        else
        {
            Debug.LogWarning("SpikeTrap: 未找到玩家对象！");
        }
    }

    private void CreateFadePanel()
    {
        // 创建一个Canvas用于显示渐隐效果
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = fadeSortingOrder;

        // 创建遮罩面板
        fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(canvasObj.transform, false);

        // 添加Image组件
        UnityEngine.UI.Image image = fadePanel.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // 初始透明
        image.raycastTarget = false; // 不阻挡点击

        // 设置为全屏
        RectTransform rectTransform = fadePanel.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;

        // 初始隐藏
        fadePanel.SetActive(false);
    }

    private IEnumerator HandleDamageAndTeleport()
    {
        isProcessingDamage = true;

        // 记录受伤时间
        lastDamageTime = Time.time;

        // 1. 锁定玩家位置（禁用移动但保持其他功能）
        LockPlayerMovement();

        // 2. 造成伤害
        if (healthManager != null)
        {
            healthManager.TakeDamage(damageAmount);
            Debug.Log($"地刺造成 {damageAmount} 点伤害");
        }

        // 检查玩家是否死亡
        if (healthManager != null && healthManager.CurrentHealth <= 0)
        {
            // 如果死亡，解锁玩家位置（让死亡动画正常播放）
            UnlockPlayerMovement();
            isProcessingDamage = false;
            yield break;
        }

        // 3. 获取最后的地面位置
        Vector2 teleportPosition = GetLastGroundPosition();

        // 4. 画面渐黑
        yield return StartCoroutine(FadeOut());

        // 5. 等待一段时间后传送
        yield return new WaitForSeconds(delayBeforeTeleport);

        // 6. 传送玩家到最后的地面位置
        if (playerTransform != null)
        {
            // 确保传送位置在地面上方一点
            Vector2 finalPosition = teleportPosition;

            // 如果传送位置是碰撞点，稍微抬高避免卡入地面
            if (finalPosition != Vector2.zero)
            {
                finalPosition += Vector2.up * 0.5f;

                // 传送时临时解锁位置进行传送
                Rigidbody2D rb = playerRigidbody;
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.velocity = Vector2.zero;
                }

                playerTransform.position = finalPosition;

                // 重新锁定位置
                if (rb != null)
                {
                    rb.isKinematic = true;
                }

                Debug.Log($"地刺传送玩家到: {finalPosition}");
            }
            else
            {
                Debug.LogWarning("地刺: 未找到最后的地面位置，使用场景起点");
                // 如果没有记录位置，尝试查找场景起点
                Transform startPoint = GameObject.FindGameObjectWithTag("StartPoint")?.transform;
                if (startPoint != null)
                {
                    Rigidbody2D rb = playerRigidbody;
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.velocity = Vector2.zero;
                    }

                    playerTransform.position = startPoint.position;

                    if (rb != null)
                    {
                        rb.isKinematic = true;
                    }
                }
            }
        }

        // 7. 等待一段时间后恢复画面
        yield return new WaitForSeconds(delayBeforeFadeIn);

        // 8. 画面恢复
        yield return StartCoroutine(FadeIn());

        // 9. 解锁玩家移动
        UnlockPlayerMovement();

        isProcessingDamage = false;

    }

    private void LockPlayerMovement()
    {
        if (playerRigidbody != null)
        {
            // 保存当前状态
            originalIsKinematic = playerRigidbody.isKinematic;
            originalGravityScale = playerRigidbody.gravityScale;

            // 锁定位置：设置为运动学，不受物理影响
            playerRigidbody.isKinematic = true;
            playerRigidbody.velocity = Vector2.zero;

            // 可选：禁用重力
            playerRigidbody.gravityScale = 0f;
        }
    }

    private void UnlockPlayerMovement()
    {
        if (playerRigidbody != null)
        {
            // 恢复原始状态
            playerRigidbody.isKinematic = originalIsKinematic;
            playerRigidbody.gravityScale = originalGravityScale;
            playerRigidbody.velocity = Vector2.zero;
        }
    }

    private Vector2 GetLastGroundPosition()
    {
        // 从TestDragonControl获取最后的地面位置
        if (playerController != null)
        {
            // 尝试通过反射获取私有字段，或者使用公共属性
            var groundPosField = typeof(TestDragonControl).GetField("lastPlayerPositionOnGround",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (groundPosField != null)
            {
                return (Vector2)groundPosField.GetValue(playerController);
            }

            // 如果添加了公共属性，可以直接使用
            // return playerController.LastPlayerPositionOnGround;
        }

        // 方法2：查找场景中的重生点标记
        GameObject respawnPoint = GameObject.FindGameObjectWithTag("RespawnPoint");
        if (respawnPoint != null)
        {
            return respawnPoint.transform.position;
        }

        // 如果都找不到，返回当前玩家位置
        if (playerTransform != null)
        {
            return playerTransform.position;
        }

        return Vector2.zero;
    }

    private IEnumerator FadeOut()
    {
        if (fadePanel == null) yield break;

        fadePanel.SetActive(true);
        UnityEngine.UI.Image image = fadePanel.GetComponent<UnityEngine.UI.Image>();

        float elapsedTime = 0f;
        Color startColor = image.color;
        Color targetColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            image.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        image.color = targetColor;
    }

    private IEnumerator FadeIn()
    {
        if (fadePanel == null) yield break;

        UnityEngine.UI.Image image = fadePanel.GetComponent<UnityEngine.UI.Image>();

        float elapsedTime = 0f;
        Color startColor = image.color;
        Color targetColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeInDuration);
            image.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        image.color = targetColor;
        fadePanel.SetActive(false);
    }

    // 在场景中显示Gizmos，方便调试
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            // 编辑模式下显示地刺范围
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}