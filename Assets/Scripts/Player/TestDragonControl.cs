using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))] //要求gameobject必须有rigidbody2D组件
public class TestDragonControl : MonoBehaviour
{
    public static TestDragonControl instance;
    public string SceneID;

    //========== 移动参数 ==========
    [Header("移动参数")]
    [SerializeField] private float maxSpeed = 7f;           // 最大移动速度
    [SerializeField] private float acceleration = 15f;      // 地面加速度
    [SerializeField] private float deceleration = 10f;      // 地面减速度
    [SerializeField] private float airControl = 0.5f;       // 空中控制系数（范围0-1，1表示与地面相同）
    [SerializeField] private KeyCode JumpKey = KeyCode.UpArrow;   // 跳跃按键

    // ========== 跳跃参数 ==========
    [Header("跳跃参数")]
    [SerializeField] private float jumpForce = 12f;         // 初始跳跃力
    [SerializeField] private float jumpCutMultiplier = 0.5f; // 松开跳跃键时的速度衰减系数
    [SerializeField] private float fallMultiplier = 2.5f;   // 下落时的重力倍率
    [SerializeField] private float lowJumpMultiplier = 2f;  // 短按跳跃时的重力倍率
    [SerializeField] private float coyoteTime = 0.1f;       // 土狼时间（离地后仍可跳跃的时间）
    [SerializeField] private float jumpBufferTime = 0.1f;   // 跳跃缓冲时间（提前按跳跃的有效时间）
    [SerializeField] private float doubleJumpForce = 10f;   // 二段跳跳跃力

    // ========== 地面检测 ==========
    [Header("地面检测设置")]
    [SerializeField] private Transform groundCheck;          // 检测点父对象
    [SerializeField] private float checkDistance = 0.1f;     // 检测距离
    [SerializeField] private LayerMask groundLayer;          // 地面层级
    [SerializeField] private LayerMask platformLayer;          // 地面层级
    [SerializeField] private float groundCheckWidth = 0.5f;  // 检测宽度

    // ========== 记录最后的地面位置 ==========
    private Vector2 lastPlayerPositionOnGround;      // 最后站在地面时的角色位置
    public Vector2 LastPlayerPositionOnGround => lastPlayerPositionOnGround;

    //  ========== 技能 ==========
    [Header("技能组件")]
    [SerializeField] private DashAbility dashAbility;
    [SerializeField] private DoubleJumpAbility doubleJumpAbility;
    [SerializeField] private WallSlide wallSlide;

    // ========== 组件引用 ==========
    private Rigidbody2D rb;        // Rigidbody2D组件
    private Collider2D col;        // 碰撞体组件
    private Animator animator;     // 动画控制器

    // ========== 输入变量 ==========
    private float horizontalInput; // 水平输入（-1到1）
    private bool jumpPressed;      // 跳跃键按下（当前帧）
    private bool jumpHeld;         // 跳跃键按住
    private bool jumpReleased;     // 跳跃键释放

    // ========== 持久化输入状态（静态变量，跨场景保存） ==========
    private static float persistentHorizontalInput = 0f; // 持久化水平输入

    // ========== 状态变量 ==========
    public bool isGrounded;       // 是否在地面
    private bool isFacingRight = true; // 是否面向右侧
    private float coyoteTimeCounter;   // 土狼时间计数器
    private float jumpBufferCounter;   // 跳跃缓冲计数器
    private bool isJumping;        // 是否正在跳跃

    // ========== 动画参数哈希 ==========
    // 使用哈希值提高动画参数查询效率
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");

    // ========== 初始化 ==========
    void Awake()
    {
        // 获取组件引用
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();

        if (instance == null)
        {
            instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            if(instance != this)
            {
                Destroy(gameObject);
            }
        }
        DontDestroyOnLoad(gameObject);
    }

    // ========== 场景加载完成回调 ==========
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景加载后，先恢复之前的输入状态
        horizontalInput = persistentHorizontalInput;
        // 重置刚体速度
        if (rb != null)
        {
            rb.velocity = new Vector2(persistentHorizontalInput * maxSpeed, 1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        GetInput();             // 获取玩家输入
        HandleCoyoteTime();     // 处理土狼时间
        HandleJumpBuffer();     // 处理跳跃缓冲
        HandleJumpCut();        // 处理跳跃中断（短按）
        UpdateAnimations();     // 更新动画状态
        CheckGround();          // 检测地面状态
    }
    void FixedUpdate()
    {
        HandleMovement();       // 处理水平移动
        HandleJump();           // 处理跳跃
        HandleGravity();        // 处理重力
    }

    // ========== 获取输入 ==========
    void GetInput()
    {
        // 正常获取当前帧输入
        horizontalInput = Input.GetAxisRaw("Horizontal");
         // 场景切换中，使用保存的输入状态
        persistentHorizontalInput = horizontalInput;

        // 获取跳跃输入状态
        jumpPressed = Input.GetKeyDown(JumpKey);   // 当前帧按下
        jumpHeld = Input.GetKey(JumpKey);          // 持续按住
        jumpReleased = Input.GetKeyUp(JumpKey);    // 当前帧释放

        // 跳跃缓冲：记录跳跃键按下的时间
        if (jumpPressed)
        {
            jumpBufferCounter = jumpBufferTime;  // 设置跳跃缓冲时间
        }
    }

    // ========== 土狼时间处理 ==========
    // 土狼时间：离开地面后的一小段时间内仍可跳跃
    void HandleCoyoteTime()
    {
        if (isGrounded)
        {
            // 在地面时重置土狼时间
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            // 不在地面时递减计数器
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    // ========== 跳跃缓冲处理 ==========
    // 跳跃缓冲：提前按下跳跃键，在落地时自动执行跳跃
    void HandleJumpBuffer()
    {
        if (jumpBufferCounter > 0)
        {
            // 递减缓冲计数器
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    // ========== 地面检测 ==========
    public void CheckGround()
    {
        bool shouldCheckPlatform = rb.velocity.y <= 0f;
        LayerMask checkLayer = groundLayer;
        if (shouldCheckPlatform)
        {
            // 包括平台层
            checkLayer = groundLayer | platformLayer;
        }
        else
        {
            // 排除平台层
            checkLayer = groundLayer;
        }

        // ========== 多射线检测 ==========
        // 计算三个检测点的位置
        Vector2 centerPos = groundCheck.position;
        Vector2 leftPos = centerPos + Vector2.left * groundCheckWidth * 0.34f;
        Vector2 rightPos = centerPos + Vector2.right * groundCheckWidth * 0.34f;

        // 发射三条射线进行检测
        RaycastHit2D centerHit = Physics2D.Raycast(centerPos, Vector2.down, checkDistance, checkLayer);
        RaycastHit2D leftHit = Physics2D.Raycast(leftPos, Vector2.down, checkDistance, checkLayer);
        RaycastHit2D rightHit = Physics2D.Raycast(rightPos, Vector2.down, checkDistance, checkLayer);

        // ========== 调试可视化 ==========
        Debug.DrawRay(centerPos, Vector2.down * checkDistance, Color.red);
        Debug.DrawRay(leftPos, Vector2.down * checkDistance, Color.green);
        Debug.DrawRay(rightPos, Vector2.down * checkDistance, Color.blue);

        // ========== 判断逻辑 ==========
        // 只要任意一条射线检测到地面，就认为角色在地面
        isGrounded = centerHit.collider != null ||
                     leftHit.collider != null ||
                     rightHit.collider != null;

        if(centerHit.collider != null)
        {
            lastPlayerPositionOnGround = transform.position;
        }

        // ========== 获取地面信息 ==========
        /*
        if (isGrounded)
        {
            // 优先使用中间的碰撞信息
            RaycastHit2D hit = centerHit.collider != null ? centerHit :
                              (leftHit.collider != null ? leftHit : rightHit);

            // 可以获取地面的法线、碰撞体等信息
            Vector2 groundNormal = hit.normal;
            float groundAngle = Vector2.Angle(groundNormal, Vector2.up);

            // 如果斜坡太陡，不算地面
            if (groundAngle > 45f) isGrounded = false;
        }
        */
    }


    void HandleMovement()
    {

        if (wallSlide.isWallJumping || (wallSlide.wasAgainstWall && !wallSlide.isAgainstWall && wallSlide.CheckWallSlideSkillEnabled()))
        {
            // 蹬墙跳时强制面向墙壁的反方向
            float wallDirection = wallSlide.wasAgainstWallDirection;

            if (wallDirection > 0 && horizontalInput == 0) // 从右侧墙壁起跳
            {
                // 应该面向左侧
                if (isFacingRight)
                {
                    Flip();
                }
            }
            else if (wallDirection < 0 && horizontalInput == 0) // 从左侧墙壁起跳
            {
                // 应该面向右侧
                if (!isFacingRight)
                {
                    Flip();
                }
            }
            if (wallSlide.isWallJumping) { 
                float expectedWallJumpSpeed = -wallDirection * wallSlide.wallJumpForceX;
    
                // 如果当前水平速度小于预期速度，设置为预期速度
                // 这确保蹬墙跳期间至少保持初始速度
                if (Mathf.Abs(rb.velocity.x) < Mathf.Abs(expectedWallJumpSpeed))
                {
                    rb.velocity = new Vector2(expectedWallJumpSpeed, rb.velocity.y);
                    return;
                }
            }
        }

        // 计算目标水平速度
        float targetSpeed = horizontalInput * maxSpeed;

        // 根据是否在地面决定加速度和减速度
        // 空中控制通常比地面控制弱
        float accelerate = isGrounded ? acceleration : acceleration * airControl;
        float decelerate = isGrounded ? deceleration : deceleration * airControl;

        // 计算当前速度与目标速度的差值
        float speedDiff = targetSpeed - rb.velocity.x;

        // 判断应该使用加速度还是减速度
        // 如果有输入（targetSpeed不为0），使用加速度；否则使用减速度
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? accelerate : decelerate;

        // 根据加速度公式 F = m * a 计算需要施加的力
        // 由于Rigidbody2D质量默认为1，可以简化为 F = a
        float movement = speedDiff * accelRate;

        // 施加水平方向的力
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        // 限制最大水平速度
        if (Mathf.Abs(rb.velocity.x) > maxSpeed)
        {
            rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x) * maxSpeed, rb.velocity.y);
        }

        // 根据移动方向翻转角色
        if (horizontalInput > 0 && !isFacingRight)
        {
            Flip();  // 向右移动但面朝左，需要翻转
        }
        else if (horizontalInput < 0 && isFacingRight)
        {
            Flip();  // 向左移动但面朝右，需要翻转
        }
    }

    void HandleJump()
    {
        // 跳跃条件判断：
        // 1. 在地面上，或者处于土狼时间内
        // 2. 跳跃缓冲有效（玩家按下了跳跃键）
        bool canNormalJump = (isGrounded || coyoteTimeCounter > 0) && jumpBufferCounter > 0;
        bool canDoubleJump = false;

        if (doubleJumpAbility != null)
        {
            canDoubleJump = !isGrounded &&
                           doubleJumpAbility.canDoubleJump == true &&
                           jumpBufferCounter > 0;
        }

        if (canNormalJump)
        {
            // 执行跳跃：直接设置垂直速度
            // 使用velocity而不是AddForce，确保每次跳跃高度一致
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);

            // 重置计数器和状态
            jumpBufferCounter = 0;      // 清除跳跃缓冲
            coyoteTimeCounter = 0;      // 清除土狼时间
            isJumping = true;           // 标记为跳跃状态
        }
        else if (canDoubleJump && ( !wallSlide.wasAgainstWall || !wallSlide.CheckWallJumpSkillEnabled()) && wallSlide.canDoubleJump)
        {
            // 执行二段跳
            rb.velocity = new Vector2(rb.velocity.x, doubleJumpForce);

            // 重置跳跃缓冲
            jumpBufferCounter = 0;

            // 消耗二段跳次数
            doubleJumpAbility.canDoubleJump = false;

            // 设置二段跳状态
            isJumping = true;
        }
    }

    // ========== 跳跃中断处理 ==========
    // 当玩家短按跳跃键时，跳得低一些
    void HandleJumpCut()
    {
        // 条件：跳跃键释放、角色正在上升、处于跳跃状态
        if (jumpReleased && rb.velocity.y > 0 && isJumping)
        {
            // 减少垂直速度，实现短跳效果
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
            isJumping = false;  // 结束跳跃状态
        }
    }

    // ========== 重力处理 ==========
    // 实现可变重力：下落时重力更大，上升时重力更小（根据是否按住跳跃键）
    void HandleGravity()
    {
        if (wallSlide.isWallSlide)
        {
            rb.gravityScale = 0f;
        }
        else if (rb.velocity.y < 0)
        {
            // 角色在下落：增加重力，让下落更快
            rb.gravityScale = fallMultiplier;
        }
        else if (rb.velocity.y > 0 && !jumpHeld)
        {
            // 角色在上升但没有按住跳跃键：中等重力（实现低跳跃）
            rb.gravityScale = lowJumpMultiplier;
        }
        else
        {
            rb.gravityScale = 1f;
        }
       
    }

    // ========== 角色翻转 ==========
    // 通过修改localScale的x值来翻转角色
    void Flip()
    {
        if (!dashAbility.isDashing) { 
            isFacingRight = !isFacingRight;  // 切换面向状态

            // 获取当前缩放
            Vector3 scale = transform.localScale;
            scale.x *= -1;  // 反转x轴缩放（负值会翻转角色）
            transform.localScale = scale;
            
            // 如果角色初始面向左侧，需要调整逻辑
        }
    }

    // ========== 动画更新 ==========
    void UpdateAnimations()
    {
        if (animator == null) return;

        // 角色只要按方向键就会播放移动动画，即使被墙壁挡住
        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f;
        animator.SetBool(IsMovingHash, isMoving);
        animator.SetBool(IsGroundedHash, isGrounded);
        animator.SetFloat(VerticalVelocityHash, rb.velocity.y);
    }

}
