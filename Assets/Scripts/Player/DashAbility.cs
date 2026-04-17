using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DashAbility : MonoBehaviour
{
    [Header("冲刺设置")]
    [SerializeField] private KeyCode dashKey = KeyCode.C;     // 冲刺按键
    [SerializeField] private float dashDistance = 3f;         // 冲刺固定距离
    [SerializeField] private float dashDuration = 0.1f;       // 冲刺动画时间（视觉效果）
    [SerializeField] private float dashCooldown = 0.5f;       // 冲刺冷却时间
    [SerializeField] private AnimationCurve dashCurve = AnimationCurve.Linear(0, 1, 1, 1); // 冲刺速度曲线
    [SerializeField] private bool maintainMomentum = true;   // 冲刺后是否保持动量

    [Header("组件引用")]
    [SerializeField] private SkillTree skillScript;  // 直接引用SkillTree脚本
    [SerializeField] private WallSlide wallSlide;

    // 组件引用
    private Rigidbody2D rb;
    private TestDragonControl playerController;
    private Collider2D col;
    private SkillInterface skillInterface;
    private Animator animator;     // 动画控制器
    private static readonly int IsDashingHash = Animator.StringToHash("IsDashing");

    // 状态变量
    private bool canDash = true;    //是否可以开始冲刺
    public bool isDashing = false;  //是否正在冲刺
    private float dashTimer = 0f;   //冲刺计时器
    private float cooldownTimer = 0f;   //冷却计时器
    private float originalGravityScale; //原始重力
    private bool isGrounded;    //是否在地面上
    private bool startcooldown; //是否可以开始冷却计时

    // 冲刺计算变量
    private Vector2 dashStartPosition;  //冲刺原始位置
    private Vector2 dashTargetPosition; //冲刺目标位置
    private Vector2 dashDirection = Vector2.right;  //冲刺方向
    private Coroutine dashCoroutine;    //冲刺协程引用，用于管理冲刺过程

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<TestDragonControl>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();

        originalGravityScale = rb.gravityScale; // 记录原始重力

        if (skillScript != null)
        {
            skillInterface = skillScript as SkillInterface;
        }
    }

    void Update()
    {
        CheckGround();
        if(!isDashing && (isGrounded || wallSlide.wasAgainstWall)) startcooldown = true;
            // 更新冷却计时器
        if (startcooldown && !canDash && !isDashing)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= dashCooldown)
            {
                canDash = true;
                cooldownTimer = 0f;
            }
        }

        // 冲刺输入检测
        if (CheckDashSkillEnabled() && canDash && Input.GetKeyDown(dashKey) && !wallSlide.isWallSlide)
        {
            StartDash();
        }

        // 如果正在冲刺，更新计时器（仅用于视觉效果）
        if (isDashing)
        {
            dashTimer += Time.deltaTime;
            if (dashTimer >= dashDuration)
            {
                dashTimer = dashDuration;
            }
        }

        UpdateAnimations();     // 更新动画状态
    }

    void CheckGround()
    {
        if (playerController != null)
        {
            isGrounded = playerController.isGrounded;
        }
    }

    bool CheckDashSkillEnabled()
    {
        // 检查技能是否可用
        if (skillInterface != null && skillInterface.IsDashSkillAvailable())
        {
            return true;
        }
        return false;
    }

    void StartDash()
    {
        // 确定冲刺方向
        DetermineDashDirection();

        // 计算冲刺目标位置
        dashStartPosition = rb.position;
        dashTargetPosition = dashStartPosition + dashDirection * dashDistance;

        // 开始冲刺协程
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
        }
        dashCoroutine = StartCoroutine(PerformDash());
    }

    void DetermineDashDirection()
    {
        // 获取水平输入
        float horizontal = Input.GetAxisRaw("Horizontal");

        // 如果有水平输入，使用输入方向
        if (Mathf.Abs(horizontal) > 0.1f)
        {
            dashDirection = new Vector2(Mathf.Sign(horizontal), 0);
        }
        else
        {
            // 没有输入时，使用角色朝向
            Vector3 localScale = transform.localScale;
            dashDirection = localScale.x > 0 ? Vector2.right : Vector2.left;
        }

        // 确保始终是水平方向
        dashDirection = new Vector2(dashDirection.x, 0);
    }

    IEnumerator PerformDash()   //协程
    {
        // 设置冲刺状态
        isDashing = true;
        canDash = false;
        dashTimer = 0f;
        startcooldown = false;

        // 禁用重力
        rb.gravityScale = 0f;

        // 重置垂直速度（确保纯水平冲刺）
        rb.velocity = new Vector2(rb.velocity.x, 0);

        // 使用FixedUpdate进行物理移动，确保与物理系统同步
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            float t = elapsedTime / dashDuration;
            float curveValue = dashCurve.Evaluate(t);

            // 计算当前位置（线性插值）
            Vector2 currentPosition = Vector2.Lerp(dashStartPosition, dashTargetPosition, t);

            // 应用移动（使用MovePosition确保平滑物理移动）
            rb.MovePosition(currentPosition);

            // 使用velocity保持速度（可选，用于其他系统检测速度）
            rb.velocity = dashDirection * (dashDistance / dashDuration) * curveValue;

            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // 确保到达精确的目标位置
        rb.MovePosition(dashTargetPosition);

        // 冲刺结束
        EndDash();
    }

    void EndDash()
    {
        // 重置冲刺状态
        isDashing = false;

        // 停止协程
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }

        // 恢复重力
        rb.gravityScale = originalGravityScale;

        // 设置冲刺后的速度
        if (maintainMomentum)
        {
            // 保留部分水平动量
            rb.velocity = dashDirection * (dashDistance / dashDuration) * 0.3f;
        }
        else
        {
            // 立即停止
            rb.velocity = Vector2.zero;
        }

    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDashing)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                // 如果冲刺中撞到墙壁，提前结束冲刺
                EndDash();
            }
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetBool(IsDashingHash, isDashing);
    }
}

