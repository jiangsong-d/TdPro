using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI相关的关联和控制,都可以在这里添加和找到
/// </summary>
public class UIModel : MonoBehaviour
{
    [NonSerialized]
    public static UIModel Inst;

    /// <summary>
    /// 开发分辨率比例
    /// </summary>
    private static readonly float EditorScreenRatio = 9 /16f;
    /// <summary>
    /// 最大适配21:9
    /// </summary>
    private static readonly float EditorScreenRatio219 = 9 /21f;
    /// <summary>
    /// 最大适配4:3
    /// </summary>
    private static readonly float EditorScreenRatio43 = 3 / 4f;

    public Transform UICanvas;
    public RectTransform UICanvasRect;
    public CanvasScaler UIScaler;

    /// <summary>
    /// 3D模型挂点
    /// </summary>
    public Transform ModelRoot;

    [Space(20)]
    /// <summary>
    /// UI摄像机
    /// </summary>
    public Camera UICamera;
    /// <summary>
    /// 所有窗口都在这一层
    /// </summary>
    public Transform NormalUIRoot;
    /// <summary>
    /// 场景层Layer(e.g. 场景HUD等所在层)
    /// </summary>
    public Transform SceneUIRoot;
    /// <summary>
    /// 常驻窗口挂点
    /// </summary>
    public Transform ConstUIRoot;

    [Space(20)]
    /// <summary>
    /// Info提示层(飘字提示等)
    /// </summary>
    public Transform InfoLayer;
    /// <summary>
    /// 更高的Info提示层(跑马灯等)
    /// </summary>
    public Transform TopInfoLayer;
    /// <summary>
    /// 系统信息提示层(跑马灯等)
    /// </summary>
    public Transform SystemInfoLayer;
    /// <summary>
    /// Top层(屏幕点击特效等)
    /// </summary>
    public Transform TopLayer;
    /// <summary>
    /// Launcer层(启动界面)
    /// </summary>
    public Transform LauncerLayer;

    /// <summary>
    /// 所有需要适配的UI节点
    /// </summary>
    public RectTransform[] NeedAdapterRects;

    /// <summary>
    /// 是否初始化完成
    /// </summary>
    public bool IsInitFinish { get; set; } = false;
    /// <summary>
    /// 屏幕尺寸
    /// </summary>
    public Vector2 ScreenSize { get; set; }
    /// <summary>
    /// 刘海厚度
    /// </summary>
    public float ScreenCutPixelX { get; set; } = 0;

        /// <summary>
    /// 刘海厚度
    /// </summary>
    public float ScreenCutPixelY { get; set; } = 0;
    /// <summary>
    /// 非正规比例下摄像机的FOV缩放比
    /// </summary>
    public float CameraFovScale { get; set; } = 1;
    /// <summary>
    /// 非正规比例下摄像机的FOV缩放比(就高缩放，4:3 pad类型)
    /// </summary>
    public float CameraFovHeightScale { get; set; } = 1;
    /// <summary>
    /// 是否为刘海屏
    /// </summary>
    public bool IsNotchScreen { get; set; } = false;
    /// <summary>
    /// 是否测试刘海屏
    /// </summary>
    public bool TestNotchScreen { get; set; } = false;

    private void Awake()
    {
        Inst = this;
    }

    public void Init()
    {
        if (Application.isPlaying)
        {
            if (ModelRoot == null)
                ModelRoot = new GameObject("ModelRoot").transform;
        }
        TestNotchScreen = Launcher.Instance.isLiuHaiPing;
        ResetData();
        StartCoroutine(RefreshUIRealSize());
    }


    /// <summary>
    /// 刷新UI实际尺寸
    /// </summary>
    /// <returns></returns>
    private IEnumerator RefreshUIRealSize()
    {
        if (IsInitFinish)
            yield return new WaitForEndOfFrame();

        // 获取CanvasScaler的参考分辨率 (1920x1080)
        var referenceResolution = UIScaler.referenceResolution;
        // 获取当前屏幕宽高
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // 计算Canvas的实际尺寸
        // 假设 Match = 1 (Height)，Canvas高度固定为 referenceResolution.y
        // Canvas宽度 = Canvas高度 * (屏幕宽 / 屏幕高)
        float canvasHeight = referenceResolution.y;
        float canvasWidth = canvasHeight * (screenWidth / screenHeight);
        
        // 如果是 Match = 0 (Width) 或者其他值，可以使用更通用的计算，但这里针对横屏 Match Height 优化
        if (UIScaler.matchWidthOrHeight == 0)
        {
            canvasWidth = referenceResolution.x;
            canvasHeight = canvasWidth * (screenHeight / screenWidth);
        }
        else if (UIScaler.matchWidthOrHeight != 1)
        {
            // 混合模式，这里简化处理，建议横屏游戏使用 Match Height (1)
            // 重新计算 scaleFactor
            float logWidth = Mathf.Log(screenWidth / referenceResolution.x, 2);
            float logHeight = Mathf.Log(screenHeight / referenceResolution.y, 2);
            float logWeightedAverage = Mathf.Lerp(logWidth, logHeight, UIScaler.matchWidthOrHeight);
            float scaleFactor = Mathf.Pow(2, logWeightedAverage);
            
            canvasWidth = screenWidth / scaleFactor; // 这里 scaleFactor 是 屏幕/Canvas 的比例? 
            // CanvasScaler 源码逻辑: scaleFactor = 屏幕像素 / Canvas像素
            // 所以 CanvasSize = ScreenSize / scaleFactor
            // 实际上 CanvasScaler 设置 Canvas.scaleFactor.
            // 我们这里反推 Canvas 宽高:
            // CanvasScaler 实际上是修改 Canvas 的 scale。
            // Canvas 宽高 * scale = Screen 宽高.
            // 所以 Canvas 宽高 = Screen 宽高 / scale.
            // 这里的 scale 是根据 match 计算出来的。
            
            // 简单起见，直接使用 RectTransform 的 rect (需要等待一帧?)
            // 既然我们在 WaitForEndOfFrame 之后，可以直接取 UICanvasRect.rect
            if (UICanvasRect != null)
            {
                canvasWidth = UICanvasRect.rect.width;
                canvasHeight = UICanvasRect.rect.height;
            }
        }

        ScreenSize = new Vector2(canvasWidth, canvasHeight);

        // 计算 Safe Area (刘海屏适配)
        Rect safeArea = Screen.safeArea;
        
        // 将 Safe Area 转换到 Canvas 坐标系
        // Scale = Canvas Height / Screen Height (在 Match Height 模式下)
        float scale = canvasHeight / screenHeight;
        
        float safeAreaLeft = safeArea.x * scale;
        float safeAreaRight = (screenWidth - safeArea.xMax) * scale;
        float safeAreaTop = (screenHeight - safeArea.yMax) * scale;
        float safeAreaBottom = safeArea.y * scale;

        // 横屏主要关注左右刘海 (X轴)
        ScreenCutPixelX = Mathf.Max(safeAreaLeft, safeAreaRight);
        // 竖直方向通常较少，但也记录
        ScreenCutPixelY = Mathf.Max(safeAreaTop, safeAreaBottom);

        IsNotchScreen = (ScreenCutPixelX > 0 || ScreenCutPixelY > 0);

#if UNITY_EDITOR
        if (TestNotchScreen)
        {
            IsNotchScreen = true;
            // 编辑器模拟刘海，假设左边或右边有遮挡
            ScreenCutPixelX = Mathf.Max(ScreenCutPixelX, 100); 
        }
#endif

        // 针对超宽屏或特殊比例的额外处理 (可选)
        // 例如：如果屏幕太宽 (21:9)，可能限制 UI 内容区域在 1920 或 2000 宽
        // 这里暂时只做 Safe Area 适配

        IsInitFinish = true;

        // 应用适配到指定节点
        for (int i = 0; i < NeedAdapterRects.Length; i++)
        {
            if (NeedAdapterRects[i] == null) continue;

            // 设置 Padding
            // offsetMin.x = Left, offsetMin.y = Bottom
            // offsetMax.x = -Right, offsetMax.y = -Top
            NeedAdapterRects[i].offsetMin = new Vector2(ScreenCutPixelX, 0); 
            NeedAdapterRects[i].offsetMax = new Vector2(-ScreenCutPixelX, 0);
            
            // 如果需要上下适配，可以取消注释下面这行
            // NeedAdapterRects[i].offsetMin = new Vector2(ScreenCutPixelX, ScreenCutPixelY);
            // NeedAdapterRects[i].offsetMax = new Vector2(-ScreenCutPixelX, -ScreenCutPixelY);
        }

        LogUtlis.Info($"UI适配完成: ScreenSize:{ScreenSize}, SafePaddingX:{ScreenCutPixelX}, SafePaddingY:{ScreenCutPixelY}, IsNotch:{IsNotchScreen}");
    }



#if UNITY_EDITOR



    private Rect _screenSafeArea = Rect.zero;
    private void Update()
    {

        if (_screenSafeArea != Screen.safeArea)
        {

            if (_screenSafeArea == Rect.zero)
            {
                _screenSafeArea = Screen.safeArea;
            }
            else
            {
                _screenSafeArea = Screen.safeArea;
                Init();
            }
        }
    }
#endif

    private void ResetData()
    {
        ScreenCutPixelX = 0;
        CameraFovScale = 1;
        CameraFovHeightScale = 1;
        IsNotchScreen = false;
    }

    public void Clear()
    {
        if (ModelRoot)
        {
            //ModelRoot.ClearChildren();
        }
        // NormalUIRoot.ClearChildren();
        // SceneUIRoot.ClearChildren();
        // InfoLayer.ClearChildren();
        // TopInfoLayer.ClearChildren();
        // SystemInfoLayer.ClearChildren();
        // TopLayer.ClearChildren();
    }

    private void OnDestroy()
    {
        Inst = null;
        Clear();
        ResetData();
        if (ModelRoot)
        {
            //ModelRoot.DestroyGameObj();
        }
    }
}
