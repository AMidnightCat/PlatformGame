using UnityEngine;
using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class InteriorScene : MonoBehaviour
{
    [Header("基础设置")]
    public string playerTag = "Player";
    public GameObject interiorRoot;
    public bool startWithInteriorVisible = false; // 初始时内部场景是否可见

    [Header("摄像头设置")]
    public CinemachineVirtualCamera exteriorCamera;
    public CinemachineVirtualCamera interiorCamera;
    public float enterCameraBlendTime = 0.5f;  // 进入时的镜头切换时间
    public float exitCameraBlendTime = 0.5f;   // 退出时的镜头切换时间

    [Header("淡入淡出设置")]
    public float enterFadeDuration = 0.5f;     // 进入时的淡入时间
    public float exitFadeDuration = 0.5f;      // 退出时的淡出时间

    [Header("瓦片地图设置")]
    public Tilemap frontMap;                   // 前方的瓦片地图（需要淡出/淡入）
    public bool startWithFrontMapVisible = true; // 初始时前瓦片地图是否可见
    public float enterFrontMapFadeDuration = 0.5f;  // 进入时前瓦片地图淡出时间
    public float exitFrontMapFadeDuration = 0.5f;   // 退出时前瓦片地图淡入时间

    private CinemachineVirtualCamera currentCamera;
    private Coroutine currentTransitionCoroutine;
    private bool isDestroyed;
    private bool isInside; // 记录当前是否在内部

    private List<Renderer> allRenderers = new List<Renderer>();
    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();
    private Dictionary<Renderer, Material> tempMaterials = new Dictionary<Renderer, Material>();

    // 瓦片地图相关变量
    private Material frontMapMaterial;
    private Material originalFrontMapMaterial;
    private bool isFrontMapFading;

    private void Start()
    {
        InitializeRenderers();
        InitializeFrontMap();

        // 根据初始设置配置内部场景的显示状态
        if (interiorRoot != null)
        {
            interiorRoot.SetActive(startWithInteriorVisible);

            // 如果初始时内部场景可见，设置透明度为1
            if (startWithInteriorVisible)
            {
                SetAllRenderersAlpha(1f);
                isInside = true;
            }
            else
            {
                SetAllRenderersAlpha(0f);
                isInside = false;
            }
        }

        // 配置摄像头优先级
        if (exteriorCamera != null && interiorCamera != null)
        {
            if (startWithInteriorVisible)
            {
                exteriorCamera.Priority = 0;
                interiorCamera.Priority = 10;
                currentCamera = interiorCamera;
            }
            else
            {
                exteriorCamera.Priority = 10;
                interiorCamera.Priority = 0;
                currentCamera = exteriorCamera;
            }
        }
        else if (exteriorCamera != null)
        {
            exteriorCamera.Priority = 10;
            currentCamera = exteriorCamera;
        }
    }

    private void InitializeFrontMap()
    {
        if (frontMap == null) return;

        // 获取或创建瓦片地图的材质
        originalFrontMapMaterial = frontMap.GetComponent<Renderer>().material;
        frontMapMaterial = new Material(originalFrontMapMaterial);
        frontMap.GetComponent<Renderer>().material = frontMapMaterial;

        // 根据初始设置设置透明度
        float initialAlpha = startWithFrontMapVisible ? 1f : 0f;
        SetFrontMapAlpha(initialAlpha);
    }

    private void InitializeRenderers()
    {
        if (interiorRoot == null) return;

        // 获取所有Renderer组件
        var renderers = interiorRoot.GetComponentsInChildren<Renderer>(true);
        allRenderers.AddRange(renderers);

        // 为每个渲染器创建临时材质
        foreach (var renderer in allRenderers)
        {
            if (renderer == null) continue;

            originalMaterials[renderer] = renderer.material;
            Material tempMat = new Material(renderer.material);
            tempMat.CopyPropertiesFromMaterial(renderer.material);
            tempMaterials[renderer] = tempMat;

            renderer.material = tempMat;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag) || isDestroyed || !gameObject.activeInHierarchy || isInside)
            return;

        // 停止当前正在进行的协程
        if (currentTransitionCoroutine != null)
            StopCoroutine(currentTransitionCoroutine);

        currentTransitionCoroutine = StartCoroutine(EnterSequence());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag) || isDestroyed || !gameObject.activeInHierarchy || !isInside)
            return;

        // 停止当前正在进行的协程
        if (currentTransitionCoroutine != null)
            StopCoroutine(currentTransitionCoroutine);

        currentTransitionCoroutine = StartCoroutine(ExitSequence());
    }

    private IEnumerator EnterSequence()
    {
        isInside = true;

        // 确保interiorRoot激活
        if (interiorRoot != null)
        {
            interiorRoot.SetActive(true);
            // 确保从当前透明度开始淡入（可能在切换过程中被打断）
            float currentAlpha = GetCurrentAlpha();
            SetAllRenderersAlpha(currentAlpha);
        }

        // 立即切换摄像头
        SwitchCamera(interiorCamera);

        // 等待一小段时间确保摄像头切换开始
        yield return null;

        // 等待摄像头切换完成
        float waitTime = 0f;
        while (waitTime < enterCameraBlendTime)
        {
            if (isDestroyed) yield break;
            waitTime += Time.deltaTime;
            yield return null;
        }

        // 并行执行内部场景淡入和前瓦片地图淡出
        if (!isDestroyed)
        {
            Coroutine interiorFade = null;
            Coroutine frontMapFade = null;

            // 内部场景淡入
            if (interiorRoot != null && interiorRoot.activeSelf)
                interiorFade = StartCoroutine(FadeRenderers(GetCurrentAlpha(), 1f, enterFadeDuration));

            // 前瓦片地图淡出
            if (frontMap != null && frontMapMaterial != null)
                frontMapFade = StartCoroutine(FadeFrontMap(GetFrontMapAlpha(), 0f, enterFrontMapFadeDuration));

            // 等待两个协程都完成
            if (interiorFade != null)
                yield return interiorFade;
            if (frontMapFade != null)
                yield return frontMapFade;
        }

        currentTransitionCoroutine = null;
    }

    private IEnumerator ExitSequence()
    {
        isInside = false;

        // 并行执行内部场景淡出和前瓦片地图淡入
        Coroutine interiorFade = null;
        Coroutine frontMapFade = null;

        // 内部场景淡出
        if (interiorRoot != null && interiorRoot.activeSelf && !isDestroyed)
            interiorFade = StartCoroutine(FadeRenderers(GetCurrentAlpha(), 0f, exitFadeDuration));

        // 前瓦片地图淡入
        if (frontMap != null && frontMapMaterial != null && !isDestroyed)
            frontMapFade = StartCoroutine(FadeFrontMap(GetFrontMapAlpha(), 1f, exitFrontMapFadeDuration));

        // 等待两个协程都完成
        if (interiorFade != null)
            yield return interiorFade;
        if (frontMapFade != null)
            yield return frontMapFade;

        // 立即切换摄像头
        SwitchCamera(exteriorCamera);

        // 等待一小段时间确保摄像头切换开始
        yield return null;

        // 等待摄像头切换完成
        float waitTime = 0f;
        while (waitTime < exitCameraBlendTime)
        {
            if (isDestroyed) yield break;
            waitTime += Time.deltaTime;
            yield return null;
        }

        // 禁用内部场景
        if (interiorRoot != null && !isDestroyed)
            interiorRoot.SetActive(false);

        currentTransitionCoroutine = null;
    }

    private IEnumerator FadeRenderers(float startAlpha, float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetAllRenderersAlpha(targetAlpha);
            yield break;
        }

        SetAllRenderersAlpha(startAlpha);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (isDestroyed) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);

            SetAllRenderersAlpha(alpha);
            yield return null;
        }

        if (!isDestroyed)
            SetAllRenderersAlpha(targetAlpha);
    }

    private IEnumerator FadeFrontMap(float startAlpha, float targetAlpha, float duration)
    {
        if (frontMapMaterial == null || duration <= 0f)
        {
            SetFrontMapAlpha(targetAlpha);
            yield break;
        }

        isFrontMapFading = true;
        SetFrontMapAlpha(startAlpha);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (isDestroyed) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);

            SetFrontMapAlpha(alpha);
            yield return null;
        }

        if (!isDestroyed)
            SetFrontMapAlpha(targetAlpha);

        isFrontMapFading = false;
    }

    private float GetCurrentAlpha()
    {
        if (allRenderers.Count == 0) return 0f;

        foreach (var renderer in allRenderers)
        {
            if (renderer != null && tempMaterials.TryGetValue(renderer, out Material mat) && mat != null)
            {
                return mat.color.a;
            }
        }
        return 0f;
    }

    private float GetFrontMapAlpha()
    {
        if (frontMapMaterial == null) return 0f;
        return frontMapMaterial.color.a;
    }

    private void SetAllRenderersAlpha(float alpha)
    {
        foreach (var renderer in allRenderers)
        {
            if (renderer != null && tempMaterials.TryGetValue(renderer, out Material mat) && mat != null)
                SetMaterialAlpha(mat, alpha);
        }
    }

    private void SetFrontMapAlpha(float alpha)
    {
        if (frontMapMaterial == null) return;

        alpha = Mathf.Clamp01(alpha);
        Color color = frontMapMaterial.color;
        color.a = alpha;
        frontMapMaterial.color = color;

        if (frontMapMaterial.HasProperty("_Color"))
            frontMapMaterial.SetColor("_Color", color);

        if (frontMapMaterial.HasProperty("_BaseColor"))
            frontMapMaterial.SetColor("_BaseColor", color);
    }

    private void SetMaterialAlpha(Material material, float alpha)
    {
        // 确保alpha在有效范围内
        alpha = Mathf.Clamp01(alpha);

        Color color = material.color;
        color.a = alpha;
        material.color = color;

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        // 如果有其他颜色属性，也可以在这里处理
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
    }

    private void SwitchCamera(CinemachineVirtualCamera targetCamera)
    {
        if (targetCamera == null) return;

        // 确保优先级正确切换
        if (currentCamera != null)
            currentCamera.Priority = 0;

        targetCamera.Priority = 10;
        currentCamera = targetCamera;
    }

    // 公共方法，允许外部手动触发进入和退出
    public void EnterInterior()
    {
        if (isDestroyed || isInside) return;

        if (currentTransitionCoroutine != null)
            StopCoroutine(currentTransitionCoroutine);

        currentTransitionCoroutine = StartCoroutine(EnterSequence());
    }

    public void ExitInterior()
    {
        if (isDestroyed || !isInside) return;

        if (currentTransitionCoroutine != null)
            StopCoroutine(currentTransitionCoroutine);

        currentTransitionCoroutine = StartCoroutine(ExitSequence());
    }

    // 获取当前是否在内部
    public bool IsInside => isInside;

    // 公共方法，手动控制瓦片地图透明度
    public void SetFrontMapAlphaManually(float alpha)
    {
        if (frontMapMaterial != null)
            SetFrontMapAlpha(alpha);
    }

    // 公共方法，手动控制内部场景透明度
    public void SetInteriorAlphaManually(float alpha)
    {
        SetAllRenderersAlpha(alpha);
    }

    private void OnDestroy()
    {
        isDestroyed = true;

        // 停止所有协程
        if (currentTransitionCoroutine != null)
            StopCoroutine(currentTransitionCoroutine);

        // 恢复内部场景的原始材质
        foreach (var kvp in tempMaterials)
        {
            if (kvp.Value != null)
            {
                if (kvp.Key != null && originalMaterials.TryGetValue(kvp.Key, out Material original))
                    kvp.Key.material = original;
                Destroy(kvp.Value);
            }
        }

        // 恢复瓦片地图的原始材质
        if (frontMap != null && originalFrontMapMaterial != null)
        {
            frontMap.GetComponent<Renderer>().material = originalFrontMapMaterial;
        }

        if (frontMapMaterial != null)
            Destroy(frontMapMaterial);

        tempMaterials.Clear();
        originalMaterials.Clear();
        allRenderers.Clear();
    }
}