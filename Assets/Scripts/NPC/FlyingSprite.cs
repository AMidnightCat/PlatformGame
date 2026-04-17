using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingSprite : MonoBehaviour
{
    [Header("跟随目标设置")]
    [SerializeField] private Transform followTarget;           // 跟随的目标（主角）
    [SerializeField] private float followDistance = 3f;        // 跟随距离（超过此距离才会移动）
    [SerializeField] private float followSpeed = 5f;           // 跟随移动速度
    [SerializeField] private float maxFollowRange = 15f;       // 最大跟随范围（超过此距离强制返回跟随）

    [Header("自动飞向目标设置")]
    [SerializeField] private string targetTag = "Collectible"; // 要飞向的物体标签
    [SerializeField] private float detectionRadius = 10f;      // 检测半径
    [SerializeField] private float flyToSpeed = 8f;            // 飞向目标的速度

    [Header("拾取设置")]
    [SerializeField] private KeyCode pickupKey = KeyCode.Mouse0; // 拾取按键（默认鼠标左键）
    [SerializeField] private float pickupRange = 1.5f;          // 拾取范围（精灵与目标距离小于此值才能拾取）
    [SerializeField] private float pickupDelay = 0.5f;          // 拾取后等待时间（避免连续拾取）
    [SerializeField] private bool destroyOnPickup = true;       // 拾取后是否销毁目标物体

    [Header("动画设置")]
    [SerializeField] private Animator animator;                // 动画控制器
    [SerializeField] private string speedParam = "Speed";      // 速度参数名
    [SerializeField] private float animationSpeedMultiplier = 1f; // 动画速度倍率

    [Header("飞行行为设置")]
    [SerializeField] private float minFlyHeight = 1f;          // 最小飞行高度
    [SerializeField] private float maxFlyHeight = 3f;          // 最大飞行高度
    [SerializeField] private float heightSmoothTime = 0.3f;    // 高度变化平滑时间

    [Header("启用设置")]
    [SerializeField] private string enableDialogueID = "village_elder_01"; // 启用精灵需要的对话ID
    [SerializeField] private bool startDisabled = true;                     // 是否开始时禁用

    // 私有变量
    private Rigidbody2D rb;
    private Transform currentTarget;            // 当前要飞向的目标
    private float lastPickupTime;               // 上次拾取时间
    private bool isFlyingToTarget;              // 是否正在飞向目标
    private float currentHeight;                // 当前飞行高度
    private Vector2 velocityRef;                // 用于平滑移动的参考值
    private float heightVelocity;               // 高度变化速度
    private bool isWaitingForPickup;            // 是否正在等待拾取
    private bool isReturningToPlayer;           // 是否正在返回玩家
    private bool isEnabled = false;              // 精灵是否已启用
    private SpriteRenderer spriteRenderer;       // 精灵渲染器
    private Collider2D[] colliders;              // 碰撞体数组

    void Start()
    {
        // 获取组件
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        colliders = GetComponents<Collider2D>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // 如果没有指定跟随目标，尝试查找主角
        if (followTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                followTarget = player.transform;
            }
            else
            {
                Debug.LogWarning("FlyingSprite: 未找到跟随目标！");
            }
        }

        // 初始化飞行高度
        currentHeight = transform.position.y;

        // 检查是否需要开始启用
        if (startDisabled)
        {
            // 初始状态为禁用
            SetEnabled(false);

            // 开始寻找目标的协程
            StartCoroutine(FindTargetRoutine());
            StartCoroutine(CheckPlayerDistanceRoutine());

            // 检查对话完成状态
            CheckDialogueCompletion();
        }
        else
        {
            // 直接启用
            SetEnabled(true);

            // 开始寻找目标的协程
            StartCoroutine(FindTargetRoutine());
            StartCoroutine(CheckPlayerDistanceRoutine());
        }
    }

    /// <summary>
    /// 设置精灵启用/禁用状态（私有方法）
    /// </summary>
    private void SetEnabled(bool enabled)
    {
        isEnabled = enabled;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = enabled;
        }

        // 启用/禁用碰撞体
        if (colliders != null)
        {
            foreach (var col in colliders)
            {
                if (col != null)
                    col.enabled = enabled;
            }
        }

        // 启用/禁用动画器
        if (animator != null)
        {
            animator.enabled = enabled;
        }

        // 如果禁用，停止移动
        if (!enabled && rb != null)
        {
            rb.velocity = Vector2.zero;
            isFlyingToTarget = false;
            isReturningToPlayer = false;
            currentTarget = null;
        }

        Debug.Log($"FlyingSprite: 精灵已{(enabled ? "启用" : "禁用")}！");
    }

    void Update()
    {
        // 如果未启用，不执行任何逻辑
        if (!isEnabled) return;

        // 更新动画
        UpdateAnimation();

        // 更新飞行高度（只在非飞向Collectible状态时更新）
        if (!isFlyingToTarget)
        {
            UpdateFlightHeight();
        }

        // 处理拾取输入
        HandlePickupInput();
    }

    void FixedUpdate()
    {
        // 如果未启用，不执行任何逻辑
        if (!isEnabled) return;

        // 决定移动行为（优先级：返回玩家 > 飞向目标 > 跟随玩家）
        if (isReturningToPlayer)
        {
            // 返回玩家
            ReturnToPlayer();
        }
        else if (isFlyingToTarget && currentTarget != null)
        {
            // 飞向目标
            FlyToTarget();
        }
        else
        {
            // 跟随主角
            FollowTarget();
        }
    }

    /// <summary>
    /// 检查对话是否完成，如果完成则启用精灵
    /// </summary>
    void CheckDialogueCompletion()
    {
        // 获取GameDataManager
        GameDataManager gameDataManager = GameDataManager.instance;

        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            // 检查NPC进度（以第一个NPC为例）
            NPCProgress npcProgress = gameDataManager.GetNPCProgress("VillageElder");

            if (npcProgress != null && npcProgress.dialogueIndex >= 1)
            {
                EnableSprite();
            }
        }
    }

    /// <summary>
    /// 启用精灵（供外部调用）
    /// </summary>
    public void EnableSprite()
    {
        if (isEnabled) return;
        SetEnabled(true);
    }

    /// <summary>
    /// 禁用精灵（供外部调用）
    /// </summary>
    public void DisableSprite()
    {
        if (!isEnabled) return;
        SetEnabled(false);
    }

    /// <summary>
    /// 检查精灵是否已启用
    /// </summary>
    public bool IsEnabled()
    {
        return isEnabled;
    }

    /// <summary>
    /// 手动启用精灵（供外部调用）
    /// </summary>
    public void ManualEnable()
    {
        EnableSprite();
    }

    // 处理拾取输入
    void HandlePickupInput()
    {
        // 检查是否按下拾取键
        if (Input.GetKeyDown(pickupKey))
        {
            // 如果正在飞向目标，尝试拾取
            if (isFlyingToTarget && currentTarget != null)
            {
                // 检查与目标的距离是否在拾取范围内
                float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

                if (distanceToTarget <= pickupRange)
                {
                    // 在拾取范围内，执行拾取
                    OnPickupTarget();
                }
                else
                {
                    // 不在拾取范围内，提示用户
                    Debug.Log($"距离目标太远 ({distanceToTarget:F1} / {pickupRange})，需要靠近才能拾取");
                    StartCoroutine(ShowPickupHint());
                }
            }
            else if (currentTarget == null)
            {
                // 没有目标，提示用户
                Debug.Log("没有可拾取的目标");
            }
        }
    }

    // 检查玩家距离的协程
    IEnumerator CheckPlayerDistanceRoutine()
    {
        while (true)
        {
            // 如果未启用，跳过
            if (!isEnabled)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // 如果玩家存在且不是正在返回玩家的状态
            if (followTarget != null && !isReturningToPlayer)
            {
                // 计算与玩家的距离
                float distanceToPlayer = Vector2.Distance(transform.position, followTarget.position);

                // 如果距离超过最大跟随范围
                if (distanceToPlayer > maxFollowRange)
                {
                    Debug.Log($"飞行精灵距离玩家太远 ({distanceToPlayer:F1} > {maxFollowRange})，强制返回");
                    isReturningToPlayer = true;
                    isFlyingToTarget = false;  // 中断飞向目标
                    currentTarget = null;       // 清除当前目标
                }
            }

            // 每0.2s检查一次
            yield return new WaitForSeconds(0.2f);
        }
    }

    // 返回玩家
    void ReturnToPlayer()
    {
        if (followTarget == null)
        {
            isReturningToPlayer = false;
            return;
        }

        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(transform.position, followTarget.position);

        // 如果距离小于跟随距离，完成返回
        if (distanceToPlayer <= followDistance)
        {
            isReturningToPlayer = false;
            Debug.Log("飞行精灵已返回玩家身边");
            return;
        }

        // 计算方向
        Vector2 direction = (followTarget.position - transform.position).normalized;

        // 计算目标速度（返回速度比普通跟随快）
        Vector2 targetVelocity = direction * (followSpeed * 1.5f);

        // 平滑移动
        rb.velocity = Vector2.SmoothDamp(rb.velocity, targetVelocity, ref velocityRef, 0.1f);

        // 根据飞行方向翻转精灵
        UpdateFacingDirection(rb.velocity.x);
    }

    // 寻找目标物体的协程
    IEnumerator FindTargetRoutine()
    {
        while (true)
        {
            // 如果未启用，跳过
            if (!isEnabled)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // 如果不在返回玩家状态，才寻找目标
            if (!isReturningToPlayer)
            {
                // 如果不在飞向目标状态，或者目标已经失效，则寻找新目标
                if (!isFlyingToTarget || currentTarget == null)
                {
                    FindNearestTarget();
                }
            }

            // 等待0.2秒再检查，减少性能消耗
            yield return new WaitForSeconds(0.2f);
        }
    }

    // 寻找最近的指定物体
    void FindNearestTarget()
    {
        // 查找所有带有指定标签的物体
        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);

        if (targets.Length == 0)
        {
            currentTarget = null;
            isFlyingToTarget = false;
            return;
        }

        // 找出最近的物体
        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;

        foreach (GameObject target in targets)
        {
            if (target == null) continue;

            float distance = Vector2.Distance(transform.position, target.transform.position);

            // 检查是否在检测半径内
            if (distance <= detectionRadius && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = target.transform;
            }
        }

        // 如果找到了有效的目标，设置为当前目标
        if (nearestTarget != null)
        {
            // 检查拾取冷却
            if (Time.time >= lastPickupTime + pickupDelay && !isWaitingForPickup)
            {
                currentTarget = nearestTarget;
                isFlyingToTarget = true;
            }
        }
        else
        {
            // 没有找到目标，回到跟随状态
            currentTarget = null;
            isFlyingToTarget = false;
        }
    }

    // 飞向目标
    void FlyToTarget()
    {
        if (currentTarget == null)
        {
            isFlyingToTarget = false;
            return;
        }

        // 计算到目标的方向
        Vector2 direction = (currentTarget.position - transform.position).normalized;
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

        // 如果距离很近（小于拾取范围的一半），减速慢行，方便玩家拾取
        if (distanceToTarget < pickupRange * 0.5f)
        {
            // 减速，让精灵停在目标附近
            rb.velocity = Vector2.SmoothDamp(rb.velocity, Vector2.zero, ref velocityRef, 0.2f);

            // 可选：显示提示信息
            if (!isWaitingForPickup)
            {
                Debug.Log($"按 {pickupKey} 拾取目标");
            }
        }
        else
        {
            // 计算目标速度
            Vector2 targetVelocity = direction * flyToSpeed;

            // 平滑移动
            rb.velocity = Vector2.SmoothDamp(rb.velocity, targetVelocity, ref velocityRef, 0.1f);
        }

        // 根据飞行方向翻转精灵
        UpdateFacingDirection(rb.velocity.x);
    }

    // 跟随主角
    void FollowTarget()
    {
        if (followTarget == null) return;

        // 计算与主角的距离
        float distanceToTarget = Vector2.Distance(transform.position, followTarget.position);

        // 如果距离超过跟随距离，才需要移动
        if (distanceToTarget > followDistance)
        {
            // 计算方向
            Vector2 direction = (followTarget.position - transform.position).normalized;

            // 计算目标速度
            Vector2 targetVelocity = direction * followSpeed;

            // 平滑移动
            rb.velocity = Vector2.SmoothDamp(rb.velocity, targetVelocity, ref velocityRef, 0.2f);

            // 根据飞行方向翻转精灵
            UpdateFacingDirection(rb.velocity.x);
        }
        else
        {
            // 在跟随距离内，缓慢减速
            rb.velocity = Vector2.SmoothDamp(rb.velocity, Vector2.zero, ref velocityRef, 0.1f);
        }
    }

    // 更新飞行高度
    void UpdateFlightHeight()
    {
        // 计算目标高度（相对于主角）
        float targetHeight = followTarget != null ? followTarget.position.y + Mathf.PingPong(Time.time * 0.5f, maxFlyHeight - minFlyHeight) : transform.position.y;

        // 平滑调整高度
        currentHeight = Mathf.SmoothDamp(transform.position.y, targetHeight, ref heightVelocity, heightSmoothTime);

        // 应用高度（保持x不变，只改变y）
        Vector3 newPosition = transform.position;
        newPosition.y = currentHeight;
        transform.position = newPosition;
    }

    // 更新精灵面向方向（只控制左右）
    void UpdateFacingDirection(float horizontalVelocity)
    {
        if (Mathf.Abs(horizontalVelocity) > 0.1f)
        {
            // 根据水平速度方向翻转
            bool shouldFaceRight = horizontalVelocity > 0;

            Vector3 scale = transform.localScale;
            if (shouldFaceRight && scale.x < 0)
            {
                scale.x = Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
            else if (!shouldFaceRight && scale.x > 0)
            {
                scale.x = -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }
    }

    // 更新动画
    void UpdateAnimation()
    {
        if (animator == null) return;

        // 根据速度大小设置动画参数
        float speed = rb.velocity.magnitude;
        animator.SetFloat(speedParam, speed);

        // 可选：设置动画速度
        animator.speed = Mathf.Lerp(0.5f, 1.5f, speed / followSpeed) * animationSpeedMultiplier;
    }

    // 拾取目标物体
    void OnPickupTarget()
    {
        if (currentTarget == null) return;

        Debug.Log($"FlyingSprite: 拾取了 {currentTarget.name}");

        // 记录拾取时间
        lastPickupTime = Time.time;

        // 播放拾取效果
        StartCoroutine(PlayPickupEffect());

        // 如果需要销毁目标物体
        if (destroyOnPickup)
        {
            // 触发目标物体上的拾取逻辑
            IPickupable pickupable = currentTarget.GetComponent<IPickupable>();
            if (pickupable != null)
            {
                pickupable.OnPickup(this.gameObject);
            }
            else
            {
                Destroy(currentTarget.gameObject);
            }
        }

        // 清除当前目标，回到跟随状态
        currentTarget = null;
        isFlyingToTarget = false;

        // 添加短暂的等待时间
        StartCoroutine(PickupCooldown());
    }

    IEnumerator PickupCooldown()
    {
        isWaitingForPickup = true;
        // 暂时禁用飞向目标功能
        isFlyingToTarget = false;
        yield return new WaitForSeconds(pickupDelay);
        isWaitingForPickup = false;
        // 恢复寻找目标
        FindNearestTarget();
    }

    // 播放拾取效果
    IEnumerator PlayPickupEffect()
    {
        // 可选：缩放效果
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;

        float elapsedTime = 0f;
        float effectDuration = 0.1f;

        while (elapsedTime < effectDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / effectDuration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < effectDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / effectDuration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    // 显示拾取提示（可选）
    IEnumerator ShowPickupHint()
    {
        // 这里可以显示UI提示或改变精灵颜色
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.color;
            renderer.color = Color.yellow;
            yield return new WaitForSeconds(0.2f);
            renderer.color = originalColor;
        }
    }

    // 公共方法：手动设置跟随目标
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    // 公共方法：手动设置要飞向的目标
    public void SetFlyTarget(Transform target)
    {
        if (!isEnabled) return;
        currentTarget = target;
        isFlyingToTarget = true;
        isReturningToPlayer = false; // 取消返回玩家状态
    }

    // 公共方法：强制返回跟随状态
    public void ReturnToFollow()
    {
        currentTarget = null;
        isFlyingToTarget = false;
        isReturningToPlayer = false;
    }

    // 公共方法：获取当前目标
    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }

    // 公共方法：是否正在飞向目标
    public bool IsFlyingToTarget()
    {
        return isFlyingToTarget;
    }

    // 公共方法：是否正在返回玩家
    public bool IsReturningToPlayer()
    {
        return isReturningToPlayer;
    }

    // 在场景中可视化检测范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, followDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxFollowRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}

// 可选：定义拾取接口，让目标物体可以自定义拾取行为
public interface IPickupable
{
    void OnPickup(GameObject picker);
}