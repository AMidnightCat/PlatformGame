using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HealthUI : MonoBehaviour
{
    [Header("爱心图像")]
    public Image heartFullPrefab;      // 满爱心预制体
    public Image heartHalfPrefab;      // 半爱心预制体（可选）
    public Image heartEmptyPrefab;     // 空爱心预制体

    [Header("UI容器")]
    public Transform heartsContainer;   // 爱心容器（Grid Layout Group）

    [Header("设置")]
    public bool useHalfHearts = false;   // 是否使用半爱心（如需要半心效果）
    public float heartSpacing = 10f;     // 爱心间距

    private List<Image> heartImages = new List<Image>();
    private HealthManager healthManager;

    void Start()
    {
        healthManager = GetComponent<HealthManager>();

        if (healthManager == null)
        {
            healthManager = FindObjectOfType<HealthManager>();
        }

        if (healthManager != null)
        {
            // 订阅血量变化事件
            healthManager.OnHealthChanged.AddListener(UpdateHealthUI);

            // 初始化UI
            CreateHearts(healthManager.MaxHealth);
            UpdateHealthUI(healthManager.CurrentHealth, healthManager.MaxHealth);
        }
        else
        {
            Debug.LogError("找不到HealthManager组件！");
        }
    }

    void OnDestroy()
    {
        if (healthManager != null)
        {
            healthManager.OnHealthChanged.RemoveListener(UpdateHealthUI);
        }
    }
    
    // 创建爱心UI
    void CreateHearts(int maxHealth)
    {
        // 清除现有爱心
        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }
        heartImages.Clear();

        // 创建新爱心
        for (int i = 0; i < maxHealth; i++)
        {
            Image heart;

            if (heartEmptyPrefab != null)
            {
                heart = Instantiate(heartEmptyPrefab, heartsContainer);
            }
            else
            {
                // 如果没有预制体，创建默认Image
                GameObject heartObj = new GameObject("Heart", typeof(Image));
                heart = heartObj.GetComponent<Image>();
                heart.transform.SetParent(heartsContainer);
                heart.rectTransform.sizeDelta = new Vector2(32, 32);
            }

            heartImages.Add(heart);
        }

        // 设置Grid Layout Group
        GridLayoutGroup grid = heartsContainer.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.cellSize = new Vector2(32, 32);
            grid.spacing = new Vector2(heartSpacing, 0);
        }
    }

    // 更新血量显示
    void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        // 如果最大血量改变，重新创建爱心
        if (heartImages.Count != maxHealth)
        {
            CreateHearts(maxHealth);
        }

        // 更新每个爱心的显示状态
        for (int i = 0; i < heartImages.Count; i++)
        {
            if (useHalfHearts)
            {
                UpdateHeartWithHalf(i, currentHealth);
            }
            else
            {
                UpdateHeartFull(i, currentHealth);
            }
        }

    }

    // 整心显示模式
    void UpdateHeartFull(int index, int currentHealth)
    {
        if (index < currentHealth)
        {
            // 满血
            if (heartFullPrefab != null && heartImages[index].sprite != heartFullPrefab.sprite)
            {
                heartImages[index].sprite = heartFullPrefab.sprite;
            }
            heartImages[index].color = Color.white;
        }
        else
        {
            // 空血
            if (heartEmptyPrefab != null && heartImages[index].sprite != heartEmptyPrefab.sprite)
            {
                heartImages[index].sprite = heartEmptyPrefab.sprite;
            }
            heartImages[index].color = Color.white;
        }
    }

    // 半心显示模式（可选）
    void UpdateHeartWithHalf(int index, int currentHealth)
    {
        // 将血量转换为2倍值（用于半心计算）
        int doubleHealth = currentHealth * 2;
        int heartValue = (index + 1) * 2;

        if (doubleHealth >= heartValue)
        {
            // 满心
            if (heartFullPrefab != null)
            {
                heartImages[index].sprite = heartFullPrefab.sprite;
            }
        }
        else if (doubleHealth >= heartValue - 1)
        {
            // 半心
            if (heartHalfPrefab != null)
            {
                heartImages[index].sprite = heartHalfPrefab.sprite;
            }
        }
        else
        {
            // 空心
            if (heartEmptyPrefab != null)
            {
                heartImages[index].sprite = heartEmptyPrefab.sprite;
            }
        }

        heartImages[index].color = Color.white;
    }

    // 播放受伤特效（可选）
    public void PlayDamageEffect()
    {
        StartCoroutine(DamageFlashEffect());
    }

    System.Collections.IEnumerator DamageFlashEffect()
    {
        // 让所有爱心变红闪烁
        foreach (var heart in heartImages)
        {
            heart.color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        foreach (var heart in heartImages)
        {
            heart.color = Color.white;
        }
    }
}