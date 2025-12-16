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
        var defaultSize = UIScaler.referenceResolution;
        var realScreenRatio = Screen.height / (float)Screen.width;
        var defaultRatio = defaultSize.y / defaultSize.x;
        if (realScreenRatio > defaultRatio)
        {
            ScreenSize = new Vector2(realScreenRatio * defaultSize.x / defaultRatio,
                                    realScreenRatio * defaultSize.y / defaultRatio);
        }
        else if (realScreenRatio < defaultRatio)
        {
            ScreenSize = new Vector2(defaultSize.x, defaultSize.x / realScreenRatio);
        }
        else
        {
            ScreenSize = defaultSize;
        }
        if (realScreenRatio >= EditorScreenRatio)
            CameraFovScale = 1 / (defaultSize.x / ScreenSize.x);
        else
            CameraFovHeightScale = 1 / (defaultSize.y / ScreenSize.y);

        if (!IsNotchScreen || IsNotchScreen && ScreenCutPixelY == 0)
        {
            //基于Screen.safeArea修正
            if (ScreenCutPixelY == 0 && (Screen.height - Screen.safeArea.height != 0 || Screen.safeArea.y > 0))
            {
                IsNotchScreen = true;
                ScreenCutPixelY = Mathf.Max(Screen.height - Screen.safeArea.height, Screen.safeArea.y);
            }
        }

#if UNITY_EDITOR
        if (TestNotchScreen)
        {
            //工具测试强制打开
            IsNotchScreen = true;
            if (ScreenCutPixelY == 0)
            {
                ScreenCutPixelY = Screen.height * 0.1f / 2;
            }
        }
#endif
        //基于标识修正
        if (IsNotchScreen && ScreenCutPixelY == 0)
        {
            ScreenCutPixelY = 160;
        }

        //大于21：9 4：3的适配尺寸时 动态加黑边控制两侧及上下。 适配背景尺寸出图，采用1680X960.
        //兼容刘海屏，如果超宽 + 刘海屏，那就看超出宽度和刘海屏宽度，取最大值
        float ScreenCutPixelX = 0.0f;
        if (realScreenRatio > EditorScreenRatio219 && ScreenSize.x > 1920)
        {
            ScreenCutPixelX = Mathf.Max(ScreenSize.x - 1080, ScreenCutPixelX);
        }
        if (realScreenRatio < EditorScreenRatio43 && ScreenSize.y > 1920)
        {
            ScreenCutPixelY = ScreenSize.y - 1920;
        }

        //#if UNITY_EDITOR
        //修正开发屏幕视图分辨率小于实际分辨率带来的误差
        //if (Screen.width < ScreenSize.x)
        //{
        //    ScreenCutPixelX *= (Screen.width / ScreenSize.x);
        //}
        //else if (ScreenSize.x < Screen.width)
        //{
        //    ScreenCutPixelX *= (ScreenSize.x / Screen.width);
        //}

        //if (Screen.height < ScreenSize.y)
        //{
        //    ScreenCutPixelY *= (Screen.height / ScreenSize.y);
        //}
        //else if (ScreenSize.y < Screen.height)
        //{
        //    ScreenCutPixelY *= (ScreenSize.y / Screen.height);
        //}
        //#endif
 
        IsInitFinish = true;

        for (int i = 0; i < NeedAdapterRects.Length; i++)
        {
            //NeedAdapterRects[i].sizeDelta = new Vector2(-ScreenCutPixelX * 2, NeedAdapterRects[i].sizeDelta.y);
            // NeedAdapterRects[i].sizeDelta = new Vector2(-ScreenCutPixelX, -ScreenCutPixelY);
            NeedAdapterRects[i].offsetMax = new Vector2(-ScreenCutPixelX, -ScreenCutPixelY/2);
            NeedAdapterRects[i].offsetMin = new Vector2(ScreenCutPixelX, 0);
            // NeedAdapterRects[i].localPosition = new Vector3 (0,-ScreenCutPixelY / 2,0);
        }

                LogUtlis.Info($"defaultSize:{defaultSize},width:{Screen.width},height:{Screen.height}," +
           $"realScreenRatio:{realScreenRatio},defaultRatio:{defaultRatio}" + $",ScreenSize:{ScreenSize}," +
           $"CameraFovScale:{CameraFovScale},CameraFovHeightScale:{CameraFovHeightScale},IsNotchScreen:{IsNotchScreen}," +
           $"safeArea:{Screen.safeArea},ScreenCutPixelX:{ScreenCutPixelX}," +
           $"sizeDelta:{ NeedAdapterRects[0].sizeDelta},anchoredPosition: { NeedAdapterRects[0].anchoredPosition}");

#if UNITY_EDITOR
        
#endif
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
