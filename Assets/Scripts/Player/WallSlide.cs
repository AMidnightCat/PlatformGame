using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallSlide : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;

    [Header("墙壁检测")]
    [SerializeField] private Transform wallCheckCenter;    // 中心检测点
    [SerializeField] private float wallCheckHeight = 0.1f;   // 垂直检测范围
    [SerializeField] private float wallCheckDepth = 0.2f;  // 水平检测深度
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask groundLayer;

    [Header("物理材质控制")]
    [SerializeField] private PhysicsMaterial2D noFrictionMaterial;  // 零摩擦力材质
    [SerializeField] private PhysicsMaterial2D normalMaterial;      // 正常材质

    [Header("技能设置")]
    [SerializeField] private float wallSlideSpeed = 1f; //墙滑速度
    [SerializeField] private float AgainstWalldelayTime = 0.2f; //墙滑缓冲时间

    [Header("组件引用")]
    [SerializeField] private SkillTree skillScript;  // 直接引用SkillTree脚本
    [SerializeField] private TestDragonControl playerController;

    [Header("蹬墙跳参数")]
    [SerializeField] private float wallJumpForceY = 12f;   // 垂直方向蹬墙跳力
    [SerializeField] public float wallJumpForceX = 12f;   // 水平方向蹬墙跳力
    [SerializeField] private float wallJumpDuration = 0.2f; //蹬墙跳冷却时间
    [SerializeField] private float doubleJumpDuration = 0.2f;   //离墙后可二段跳的间隔时间（优先蹬墙跳的时间，应≥墙滑缓冲时间）
    [SerializeField] private KeyCode JumpKey = KeyCode.UpArrow;

    private SkillInterface skillInterface;
    public bool isAgainstWall;
    public bool canWallJump = false;
    private bool isGrounded;
    public bool wasAgainstWall;
    private float wasAgainstWallTimer = 0f;
    public bool isWallSlide = false;
    public float wasAgainstWallDirection = 0f;

    public bool isWallJumping = false;
    public bool canDoubleJump = true;
    private float wallJumpTimer = 0f;
    private float doubleJumpTimer = 0f;

    private Animator animator;     // 动画控制器
    private static readonly int IsWallSlideHash = Animator.StringToHash("IsWallSlide");

    void Awake()
    {
        if (skillScript != null)
        {
            skillInterface = skillScript as SkillInterface;
        }
        
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 检测墙壁
        CheckWallContact();

        if (isWallJumping)
        {
            isWallSlide = false;
            wallJumpTimer -= Time.deltaTime;
            doubleJumpTimer -= Time.deltaTime;

            if (doubleJumpTimer <= 0f)
            {
                canDoubleJump = true;
            }

            if (wallJumpTimer <= 0f)
            {
                isWallJumping = false;
            }
        }

        // 处理贴墙状态
        HandleWallStick();

        HandleWallSlide();

        UpdateAnimations();
    }


    void CheckWallContact()
    {
        // 检测左侧墙壁
        RaycastHit2D leftHit = Physics2D.BoxCast(
            wallCheckCenter.position,
            new Vector2(wallCheckDepth, wallCheckHeight),
            0f,
            Vector2.left,
            wallCheckDepth,
            wallLayer
        );

        // 检测右侧墙壁
        RaycastHit2D rightHit = Physics2D.BoxCast(
            wallCheckCenter.position,
            new Vector2(wallCheckDepth, wallCheckHeight),
            0f,
            Vector2.right,
            wallCheckDepth,
            wallLayer
        );

        Debug.DrawRay(wallCheckCenter.position, Vector2.left * wallCheckDepth, Color.yellow);
        Debug.DrawRay(wallCheckCenter.position, Vector2.right * wallCheckDepth, Color.yellow);

        // 判断是否贴墙（基于输入方向）
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        bool pressingLeft = horizontalInput < -0.1f;
        bool pressingRight = horizontalInput > 0.1f;

        if (isAgainstWall)
        {
            wasAgainstWall = true;
            if (pressingLeft) wasAgainstWallDirection = -1;
            if (pressingRight) wasAgainstWallDirection = 1;  
        }

        // 处理蹬墙跳输入（在Update中检测按键）
        if (wasAgainstWall && !isGrounded && CheckWallJumpSkillEnabled())
        {
            if (Input.GetKeyDown(JumpKey) && !isWallJumping)
            {
                PerformWallJump();
            }
        }

        // 条件：面向墙壁且有输入，同时不在地面
        isGrounded = playerController.isGrounded;
        isAgainstWall = !isGrounded && (
            (leftHit.collider != null && pressingLeft) ||
            (rightHit.collider != null && pressingRight)
        );

        if (wasAgainstWall)
        {
            wasAgainstWallTimer += Time.deltaTime;

            if (wasAgainstWallTimer >= AgainstWalldelayTime)
            {
                wasAgainstWall = false;
                wasAgainstWallTimer = 0f; // 重置计时器
                wasAgainstWallDirection = 0f;
            }
        }

    }

    private void PerformWallJump()
    {
        if (!CheckWallJumpSkillEnabled()) return;

        // 设置蹬墙跳速度
        rb.velocity = new Vector2(rb.velocity.x, wallJumpForceY);

        // 设置蹬墙跳状态
        isWallJumping = true;
        canDoubleJump = false;
        wallJumpTimer = wallJumpDuration;
        doubleJumpTimer = doubleJumpDuration;

        // 重置相关状态
        wasAgainstWall = false;
        wasAgainstWallTimer = 0f;
    }

    void HandleWallStick()
    {
        if (!CheckWallSlideSkillEnabled())
        {
            isWallSlide = false;
            // 当开始贴墙时，切换到零摩擦力材质
            if (!isGrounded)
            {
                col.sharedMaterial = noFrictionMaterial;
            }

            // 当离开墙壁时，恢复正常材质
            if (isGrounded)
            {
                col.sharedMaterial = noFrictionMaterial;
            }
        }
        else
        {
            // 当离开墙壁时，恢复正常材质
            if (!isAgainstWall || isGrounded)
            {
                isWallSlide = false;
            }

            if (!isGrounded)
            {
                col.sharedMaterial = noFrictionMaterial;
            }

            // 当离开墙壁时，恢复正常材质
            if (isGrounded)
            {
                col.sharedMaterial = noFrictionMaterial;
            }
        }

    }

    void HandleWallSlide()
    {
        if (!CheckWallSlideSkillEnabled()) return;

        if (isWallJumping) return;

        if (isAgainstWall)
        {
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);

            isWallSlide = true;
        }
    }

    public bool CheckWallJumpSkillEnabled()
    {
        // 检查技能是否可用
        if (skillInterface != null && skillInterface.IsWallJumpSkillAvailable())
        {
            return true;
        }
        return false;
    }

    public bool CheckWallSlideSkillEnabled()
    {
        // 检查技能是否可用
        if (skillInterface != null && skillInterface.IsWallSlideSkillAvailable())
        {
            return true;
        }
        return false;
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetBool(IsWallSlideHash, isWallSlide);
    }
}