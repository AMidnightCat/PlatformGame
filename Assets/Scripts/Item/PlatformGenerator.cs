using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlatformGenerator : MonoBehaviour, IPickupable
{
    [Header("平台生成设置")]
    [SerializeField] private Tilemap targetTilemap;              // 目标瓦片地图（PlatformMap）
    [SerializeField] private TileBase platformTile;              // 平台瓦片
    [SerializeField] private Vector3Int platformSize = new Vector3Int(3, 1, 0); // 平台大小（宽3，高1）
    [SerializeField] private Vector2Int platformOffset = new Vector2Int(0, 0); // 平台生成偏移（相对于物品位置）

    [Header("视觉效果")]
    [SerializeField] private GameObject generateEffect;          // 生成特效
    [SerializeField] private AudioClip generateSound;           // 生成音效

    [Header("物品使用后设置")]
    [SerializeField] private Sprite usedSprite;                 // 使用后的图片
    [SerializeField] private string usedTag = "Untagged";       // 使用后的标签

    private bool isUsed = false;
    private Vector3Int generatedPlatformPosition;                // 记录生成的平台位置
    private SpriteRenderer spriteRenderer;
    private string originalTag;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalTag = gameObject.tag;

        // 如果没有设置目标瓦片地图，自动查找
        if (targetTilemap == null)
        {
            GameObject platformMap = GameObject.Find("PlatformMap");
            if (platformMap != null)
            {
                targetTilemap = platformMap.GetComponent<Tilemap>();
            }
            else
            {
                Debug.LogWarning("CollectiblePlatformGenerator: 未找到 PlatformMap！");
            }
        }

        // 如果没有设置平台瓦片，尝试使用默认瓦片
        if (platformTile == null && targetTilemap != null)
        {
            // 尝试获取Tilemap中的第一个瓦片作为默认
            BoundsInt bounds = targetTilemap.cellBounds;
            foreach (var position in bounds.allPositionsWithin)
            {
                TileBase tile = targetTilemap.GetTile(position);
                if (tile != null)
                {
                    platformTile = tile;
                    break;
                }
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
            UseCollectible();
            Debug.Log($"FlyingSprite 拾取了道具，在位置 {generatedPlatformPosition} 生成了平台");
        }
    }

    // 物品使用逻辑
    void UseCollectible()
    {
        if (isUsed) return;

        isUsed = true;

        // 生成平台
        bool platformGenerated = GeneratePlatform();

        if (platformGenerated)
        {
            // 播放生成特效
            PlayGenerateEffect();

            // 播放生成音效
            PlayGenerateSound();

            // 改变物品外观
           // ChangeItemAppearance();

            // 改变标签
            ChangeTag();

            Debug.Log($"平台已生成在瓦片坐标: {generatedPlatformPosition}");
        }
        else
        {
            Debug.LogWarning("平台生成失败！");
        }
    }

    // 生成平台
    bool GeneratePlatform()
    {
        if (targetTilemap == null)
        {
            Debug.LogError("目标 Tilemap 未设置！");
            return false;
        }

        if (platformTile == null)
        {
            Debug.LogError("平台瓦片未设置！");
            return false;
        }

        // 获取物品在世界坐标中的位置
        Vector3 worldPosition = transform.position;

        // 将世界坐标转换为瓦片坐标
        Vector3Int tilePosition = targetTilemap.WorldToCell(worldPosition);

        // 应用偏移
        Vector3Int startPosition = tilePosition + new Vector3Int(platformOffset.x, platformOffset.y, 0);

        // 记录生成位置
        generatedPlatformPosition = startPosition;

        // 检查是否已经有瓦片（避免重复生成）
        bool hasExistingTile = false;
        for (int x = 0; x < platformSize.x; x++)
        {
            for (int y = 0; y < platformSize.y; y++)
            {
                Vector3Int checkPos = startPosition + new Vector3Int(x, y, 0);
                if (targetTilemap.GetTile(checkPos) != null)
                {
                    hasExistingTile = true;
                    break;
                }
            }
        }

        if (hasExistingTile)
        {
            Debug.LogWarning("目标位置已有平台，跳过生成");
            return false;
        }

        // 生成平台瓦片
        for (int x = 0; x < platformSize.x; x++)
        {
            for (int y = 0; y < platformSize.y; y++)
            {
                Vector3Int position = startPosition + new Vector3Int(x, y, 0);
                targetTilemap.SetTile(position, platformTile);
            }
        }

        // 刷新Tilemap
        targetTilemap.RefreshAllTiles();

        return true;
    }

    // 播放生成特效
    void PlayGenerateEffect()
    {
        if (generateEffect != null)
        {
            // 在平台生成位置播放特效
            Vector3 effectPosition = targetTilemap.GetCellCenterWorld(generatedPlatformPosition);
            Instantiate(generateEffect, effectPosition, Quaternion.identity);
        }
    }

    // 播放生成音效
    void PlayGenerateSound()
    {
        if (generateSound != null)
        {
            AudioSource.PlayClipAtPoint(generateSound, transform.position);
        }
    }

    // 改变物品外观
    void ChangeItemAppearance()
    {
        if (usedSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = usedSprite;
        }
        else
        {
            // 如果没有设置使用后图片，让物品半透明表示已使用
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 0.5f;
                spriteRenderer.color = color;
            }
        }
    }

    // 改变标签
    void ChangeTag()
    {
        gameObject.tag = usedTag;
    }

    // 公共方法：重置物品（如果需要重新使用）
    public void ResetItem()
    {
        isUsed = false;
        gameObject.tag = originalTag;

        if (spriteRenderer != null)
        {
            if (usedSprite != null)
            {
                // 如果有原始图片，恢复到原始图片
                Sprite originalSprite = Resources.Load<Sprite>("OriginalSprite"); // 需要保存原始图片
                if (originalSprite != null)
                    spriteRenderer.sprite = originalSprite;
            }

            // 恢复透明度
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        Debug.Log("物品已重置");
    }

    // 公共方法：获取生成的平台位置
    public Vector3Int GetGeneratedPlatformPosition()
    {
        return generatedPlatformPosition;
    }

    // 公共方法：检查是否已使用
    public bool IsUsed()
    {
        return isUsed;
    }

    // 在编辑器中显示生成范围（用于调试）
    void OnDrawGizmosSelected()
    {
        if (targetTilemap != null && !Application.isPlaying)
        {
            // 获取物品的世界坐标
            Vector3 worldPos = transform.position;
            Vector3Int tilePos = targetTilemap.WorldToCell(worldPos);
            Vector3Int startPos = tilePos + new Vector3Int(platformOffset.x, platformOffset.y, 0);

            // 计算生成区域的世界坐标范围
            Vector3 cellSize = targetTilemap.cellSize;
            Vector3 startWorldPos = targetTilemap.GetCellCenterWorld(startPos);
            Vector3 endWorldPos = targetTilemap.GetCellCenterWorld(startPos + new Vector3Int(platformSize.x - 1, platformSize.y - 1, 0));

            // 绘制半透明矩形显示生成区域
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Vector3 center = (startWorldPos + endWorldPos) / 2;
            Vector3 size = new Vector3(
                platformSize.x * cellSize.x,
                platformSize.y * cellSize.y,
                0.1f
            );
            Gizmos.DrawCube(center, size);

            // 绘制边框
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, size);
        }
    }
}