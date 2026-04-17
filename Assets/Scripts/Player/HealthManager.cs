using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HealthManager : MonoBehaviour
{
    [Header("血量设置")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth = 3;
    [SerializeField] private int healthCollectionPerHeart = 1; // 每个收集物增加多少血量上限

    [Header("事件")]
    public UnityEvent<int, int> OnHealthChanged;   // 参数：当前血量, 最大血量
    public UnityEvent<int> OnMaxHealthIncreased;    // 参数：新的最大血量
    public UnityEvent OnDeath;

    private GameDataManager gameDataManager;

    // 属性
    public int CurrentHealth
    {
        get => currentHealth;
        private set
        {
            currentHealth = Mathf.Clamp(value, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            SaveHealthToGameData();

            if (currentHealth <= 0)
            {
                OnDeath?.Invoke();
            }
        }
    }

    public int MaxHealth
    {
        get => maxHealth;
        private set
        {
            maxHealth = Mathf.Max(1, value);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            SaveHealthToGameData();
        }
    }

    void Awake()
    {
        gameDataManager = GameDataManager.instance;

        // 如果有存档数据，从存档中读取血量
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            LoadHealthFromGameData();
        }
    }

    void Start()
    {
        // 初始化UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    #region 血量操作接口

    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;
        CurrentHealth -= damage;
        Debug.Log($"受到 {damage} 点伤害，当前血量：{CurrentHealth}/{MaxHealth}");
    }

    public void Heal(int healAmount)
    {
        if (healAmount <= 0) return;
        CurrentHealth += healAmount;
        Debug.Log($"恢复 {healAmount} 点生命，当前血量：{CurrentHealth}/{MaxHealth}");
    }

    public void FullHeal()
    {
        CurrentHealth = maxHealth;
        Debug.Log($"完全恢复，当前血量：{CurrentHealth}/{MaxHealth}");
    }

    public void IncreaseMaxHealth(int increaseAmount, bool healWhenIncrease = true)
    {
        if (increaseAmount <= 0) return;

        int oldMax = maxHealth;
        MaxHealth += increaseAmount;

        OnMaxHealthIncreased?.Invoke(maxHealth);

        if (healWhenIncrease)
        {
            // 增加的血量等于增加的上限值
            CurrentHealth += increaseAmount;
        }

        // 更新存档中的生命收集物数量
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.HealthCollection = maxHealth - 3;
        }

        Debug.Log($"血量上限增加 {increaseAmount}，当前上限：{MaxHealth}");
    }


    // 设置血量
    public void SetHealth(int health, int maxHealthValue)
    {
        maxHealth = Mathf.Max(1, maxHealthValue);
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"血量设置为：{CurrentHealth}/{MaxHealth}");
    }

    #endregion

    #region 存档相关

    private void SaveHealthToGameData()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.playerHealth = currentHealth;
            gameDataManager.CurrentGameData.playerMaxHealth = maxHealth;
        }
    }

    private void LoadHealthFromGameData()
    {
        if (gameDataManager.CurrentGameData != null)
        {
            currentHealth = gameDataManager.CurrentGameData.playerHealth;
            maxHealth = gameDataManager.CurrentGameData.playerMaxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            Debug.Log($"从存档加载血量：{CurrentHealth}/{MaxHealth}");
        }
    }

    #endregion
}
