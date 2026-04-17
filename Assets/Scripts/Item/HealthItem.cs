using UnityEngine;

public class HealthItem : MonoBehaviour, IPickupable
{
    [Header("恢复设置")]
    public int healAmount = 1;            // 恢复量
    public AudioClip collectSound;
    public ParticleSystem collectEffect;

    [Header("图片设置")]
    public Sprite usedSprite;              // 使用后的图片（空血瓶）
    public Sprite normalSprite;            // 正常状态图片（可选，用于重置）

    [Header("拾取设置")]
    [SerializeField] private KeyCode pickupKey = KeyCode.F; // 拾取按键

    private HealthManager healthManager;
    private SpriteRenderer spriteRenderer;
    private bool isUsed = false;           // 是否已使用

    void Start()
    {
        healthManager = FindObjectOfType<HealthManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 如果没有设置正常图片，使用当前图片
        if (normalSprite == null && spriteRenderer != null)
        {
            normalSprite = spriteRenderer.sprite;
        }
    }

    void OnTriggerStay2D(Collider2D other)  // 使用Stay而不是Enter，以便持续检测按键
    {
        if (other.CompareTag("Player") && healthManager != null  && !isUsed)
        {
            // 检测按键按下
            if (Input.GetKeyDown(pickupKey))
            {
                UseHealthItem();
            }
        }
        
    }

    // 实现 IPickupable 接口，供 FlyingSprite 调用
    public void OnPickup(GameObject picker)
    {
        if (isUsed) return;

        // 检查是否是 FlyingSprite 拾取的
        if (picker.GetComponent<FlyingSprite>() != null)
        {
            UseHealthItem();
            Debug.Log($"FlyingSprite 拾取了回血道具，恢复 {healAmount} 点生命");
        }
    }

    void UseHealthItem()
    {
        if (isUsed) return;

        // 标记为已使用
        isUsed = true;

        // 恢复血量
        healthManager.Heal(healAmount);

        gameObject.tag = "Untagged";

        // 播放音效
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        // 播放特效
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // 改变图片
        if (usedSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = usedSprite;
        }

        Debug.Log($"使用生命回复道具，恢复 {healAmount} 点生命");

        // 不销毁物体，只是标记为已使用
    }

}