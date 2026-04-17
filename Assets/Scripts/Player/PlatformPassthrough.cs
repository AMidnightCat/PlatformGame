using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 该实现方法要保证上下两个平台间隙大于人物碰撞体高度
public class OneWayPlatformController : MonoBehaviour
{
    [Header("平台设置")]
    [SerializeField] private Collider2D col;    // 玩家碰撞体
    [SerializeField] private KeyCode downKey = KeyCode.DownArrow;  // 下穿按键
    [SerializeField] private float minIgnoreTime = 0f;   // 最小忽略时间

    [Header("平台组件引用")]
    [SerializeField] private CompositeCollider2D platformCollider;  // 平台复合碰撞体

    // 状态
    private bool isPassingThrough = false;
    private bool isFullySeparated = true;
    private float ignoreStartTime = 0f;
    private bool timeConditionMet = false;

    void Start()
    {
        // 初始化：确保开始时碰撞正常
        if (col != null && platformCollider != null)
        {
            Physics2D.IgnoreCollision(col, platformCollider, false);
        }
    }

    void OnEnable()
    {
        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // 取消订阅场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 重新查找平台碰撞体
        GameObject platform = GameObject.FindGameObjectWithTag("Platform");
        platformCollider = platform.GetComponent<CompositeCollider2D>();

        // 重置碰撞状态
        if (col != null && platformCollider != null)
        {
            Physics2D.IgnoreCollision(col, platformCollider, false);
            isPassingThrough = false;
            isFullySeparated = true;
            timeConditionMet = false;
        }
    }


    void Update()
    {
        // 检测按键输入
        HandleInput();

        // 检查分离状态
        CheckSeparation();

        // 检测时间条件
        CheckTimeCondition();
    }

    void HandleInput()
    {
        // 按下S键开始下穿
        if (Input.GetKeyDown(downKey))
        {
            StartPassingThrough();
        }

        // 松开S键且已完全分离时恢复碰撞
        if (Input.GetKeyUp(downKey) && CanRestoreCollision())
        {
            StopPassingThrough();
        }
    }

    void StartPassingThrough()
    {

        // 忽略碰撞
        Physics2D.IgnoreCollision(col, platformCollider, true);
        isPassingThrough = true;
        isFullySeparated = false;
        timeConditionMet = false;  // 重置时间条件
        ignoreStartTime = Time.time;  // 记录开始时间

    }

    void StopPassingThrough()
    {
        // 恢复碰撞
        Physics2D.IgnoreCollision(col, platformCollider, false);
        isPassingThrough = false;
    }

    void CheckSeparation()
    {
        if (!isPassingThrough) return;

        if (col != null && platformCollider != null)
        {
            // 原始方法
            // bool stillTouching = playerCollider.IsTouching(platformCollider);

            // 使用Physics2D.Distance获取两个碰撞体之间的距离信息
            ColliderDistance2D colliderDistance = Physics2D.Distance(col, platformCollider);

            // 设置一个阈值来判断是否"接触"
            float touchThreshold = 0.01f; // 调整这个值来控制检测范围

            // 如果距离小于阈值，认为还在"接触"
            bool stillTouching = colliderDistance.distance < touchThreshold;

            if (!stillTouching)
            {
                isFullySeparated = true;

                // 同时检查时间条件
                if (!Input.GetKey(downKey) && timeConditionMet)
                {
                    StopPassingThrough();
                }
            }
            else
            {
                isFullySeparated = false;
            }
        }
    }

    // 检查时间条件
    void CheckTimeCondition()
    {
        if (!isPassingThrough) return;

        // 达到最小忽略时间时设置标志
        if (!timeConditionMet && Time.time - ignoreStartTime >= minIgnoreTime)
        {
            timeConditionMet = true;

            // 如果已经分离且松开按键，恢复碰撞
            if (isFullySeparated && !Input.GetKey(downKey))
            {
                StopPassingThrough();
            }
        }
    }

    // 统一判断恢复条件
    bool CanRestoreCollision()
    {
        return isFullySeparated && timeConditionMet;
    }
}