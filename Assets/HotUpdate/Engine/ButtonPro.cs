using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections;

/// <summary>
/// Button 增强版本
/// 直接继承 Button，集成常用功能：点击音效、防连点、点击动画、长按支持等
public class ButtonPro : Button
{
    #region 点击音效配置

    [Header("=== 点击音效 ===")]
    [SerializeField] private bool m_EnableClickSound = true;
    [SerializeField] private string m_ClickSoundPath = "Audio/UI/Click";

    #endregion

    #region 防连点配置

    [Header("=== 防连点设置 ===")]
    [SerializeField] private bool m_EnableClickInterval = true;
    [SerializeField] private float m_ClickInterval = 0.5f; // 点击间隔（秒）
    private float m_LastClickTime = 0f;

    #endregion

    #region 点击动画配置

    [Header("=== 点击动画 ===")]
    [SerializeField] private bool m_EnableClickAnimation = true;
    [SerializeField] private ClickAnimationType m_AnimationType = ClickAnimationType.Scale;
    [SerializeField] private float m_ScaleAmount = 0.9f; // 缩放到原大小的90%
    [SerializeField] private float m_AnimationDuration = 0.1f;
    
    private Vector3 m_OriginalScale;
    private Coroutine m_AnimationCoroutine;

    #endregion

    #region 长按支持

    [Header("=== 长按功能 ===")]
    [SerializeField] private bool m_EnableLongPress = false;
    [SerializeField] private float m_LongPressDuration = 0.8f; // 长按触发时间
    [SerializeField] private float m_LongPressInterval = 0.2f; // 长按重复间隔
    
    [Space(5)]
    public UnityEvent OnLongPressStart = new UnityEvent(); // 长按开始
    public UnityEvent OnLongPressing = new UnityEvent();   // 长按持续触发
    public UnityEvent OnLongPressEnd = new UnityEvent();   // 长按结束
    
    private bool m_IsPointerDown = false;
    private float m_PointerDownTime = 0f;
    private bool m_IsLongPressing = false;
    private Coroutine m_LongPressCoroutine;

    #endregion

    #region 红点提示

    [Header("=== 红点提示 ===")]
    [SerializeField] private bool m_ShowRedDot = false;
    [SerializeField] private GameObject m_RedDotObj;

    #endregion

    #region 额外回调

    [Header("=== 额外回调 ===")]
    public UnityEvent OnClickDown = new UnityEvent();   // 按下
    public UnityEvent OnClickUp = new UnityEvent();     // 抬起

    #endregion

    #region 生命周期

    protected override void Awake()
    {
        base.Awake();
        m_OriginalScale = transform.localScale;
    }

    protected override void Start()
    {
        base.Start();
        UpdateRedDot();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // 重置状态
        m_IsPointerDown = false;
        m_IsLongPressing = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // 停止所有协程
        StopAllCoroutines();
        m_AnimationCoroutine = null;
        m_LongPressCoroutine = null;
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (Application.isPlaying)
        {
            UpdateRedDot();
        }
    }
#endif

    #endregion

    #region 重写点击事件

    public override void OnPointerClick(PointerEventData eventData)
    {
        // 防连点检测
        if (m_EnableClickInterval)
        {
            float currentTime = Time.unscaledTime;
            if (currentTime - m_LastClickTime < m_ClickInterval)
            {
                LogUtlis.Info($"按钮 {gameObject.name} 点击过快，已忽略");
                return;
            }
            m_LastClickTime = currentTime;
        }

        // 长按模式下，如果已经触发长按则不触发普通点击
        if (m_EnableLongPress && m_IsLongPressing)
        {
            return;
        }

        // 播放点击音效
        if (m_EnableClickSound && !string.IsNullOrEmpty(m_ClickSoundPath))
        {
            PlayClickSound();
        }

        // 执行点击动画
        if (m_EnableClickAnimation)
        {
            PlayClickAnimation();
        }

        // 调用原生点击
        base.OnPointerClick(eventData);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        
        m_IsPointerDown = true;
        m_PointerDownTime = Time.unscaledTime;
        
        OnClickDown?.Invoke();

        // 启动长按检测
        if (m_EnableLongPress)
        {
            if (m_LongPressCoroutine != null)
            {
                StopCoroutine(m_LongPressCoroutine);
            }
            m_LongPressCoroutine = StartCoroutine(LongPressCoroutine());
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        
        m_IsPointerDown = false;
        
        OnClickUp?.Invoke();

        // 停止长按
        if (m_EnableLongPress)
        {
            if (m_LongPressCoroutine != null)
            {
                StopCoroutine(m_LongPressCoroutine);
                m_LongPressCoroutine = null;
            }

            if (m_IsLongPressing)
            {
                m_IsLongPressing = false;
                OnLongPressEnd?.Invoke();
            }
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        
        // 指针移出时停止长按
        if (m_EnableLongPress && m_IsPointerDown)
        {
            m_IsPointerDown = false;
            
            if (m_LongPressCoroutine != null)
            {
                StopCoroutine(m_LongPressCoroutine);
                m_LongPressCoroutine = null;
            }

            if (m_IsLongPressing)
            {
                m_IsLongPressing = false;
                OnLongPressEnd?.Invoke();
            }
        }
    }

    #endregion

    #region 长按协程

    private IEnumerator LongPressCoroutine()
    {
        // 等待长按触发时间
        yield return new WaitForSecondsRealtime(m_LongPressDuration);

        if (m_IsPointerDown && interactable)
        {
            m_IsLongPressing = true;
            OnLongPressStart?.Invoke();

            // 持续触发
            while (m_IsPointerDown)
            {
                OnLongPressing?.Invoke();
                yield return new WaitForSecondsRealtime(m_LongPressInterval);
            }
        }
    }

    #endregion

    #region 点击动画

    private void PlayClickAnimation()
    {
        if (m_AnimationCoroutine != null)
        {
            StopCoroutine(m_AnimationCoroutine);
        }

        switch (m_AnimationType)
        {
            case ClickAnimationType.Scale:
                m_AnimationCoroutine = StartCoroutine(ScaleAnimation());
                break;
            case ClickAnimationType.Punch:
                m_AnimationCoroutine = StartCoroutine(PunchAnimation());
                break;
        }
    }

    private IEnumerator ScaleAnimation()
    {
        float elapsed = 0f;
        Vector3 targetScale = m_OriginalScale * m_ScaleAmount;

        // 缩小
        while (elapsed < m_AnimationDuration / 2)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (m_AnimationDuration / 2);
            transform.localScale = Vector3.Lerp(m_OriginalScale, targetScale, t);
            yield return null;
        }

        // 恢复
        elapsed = 0f;
        while (elapsed < m_AnimationDuration / 2)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (m_AnimationDuration / 2);
            transform.localScale = Vector3.Lerp(targetScale, m_OriginalScale, t);
            yield return null;
        }

        transform.localScale = m_OriginalScale;
        m_AnimationCoroutine = null;
    }

    private IEnumerator PunchAnimation()
    {
        float elapsed = 0f;
        Vector3 punchScale = m_OriginalScale * 1.1f; // 放大到110%

        // 放大
        while (elapsed < m_AnimationDuration / 3)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (m_AnimationDuration / 3);
            transform.localScale = Vector3.Lerp(m_OriginalScale, punchScale, t);
            yield return null;
        }

        // 缩小一点
        elapsed = 0f;
        Vector3 shrinkScale = m_OriginalScale * 0.95f;
        while (elapsed < m_AnimationDuration / 3)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (m_AnimationDuration / 3);
            transform.localScale = Vector3.Lerp(punchScale, shrinkScale, t);
            yield return null;
        }

        // 恢复
        elapsed = 0f;
        while (elapsed < m_AnimationDuration / 3)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (m_AnimationDuration / 3);
            transform.localScale = Vector3.Lerp(shrinkScale, m_OriginalScale, t);
            yield return null;
        }

        transform.localScale = m_OriginalScale;
        m_AnimationCoroutine = null;
    }

    #endregion

    #region 点击音效

    private void PlayClickSound()
    {
        // TODO: 接入音效管理器
        // AudioManager.Instance.PlaySound(m_ClickSoundPath);
        // LogUtlis.Info($"播放按钮音效: {m_ClickSoundPath}");
    }

    #endregion

    #region 红点管理

    private void UpdateRedDot()
    {
        if (m_RedDotObj != null)
        {
            m_RedDotObj.SetActive(m_ShowRedDot);
        }
    }

    /// <summary>
    /// 设置红点显示
    /// </summary>
    public void SetRedDot(bool show)
    {
        m_ShowRedDot = show;
        UpdateRedDot();
    }

    /// <summary>
    /// 设置红点对象
    /// </summary>
    public void SetRedDotObject(GameObject redDotObj)
    {
        m_RedDotObj = redDotObj;
        UpdateRedDot();
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 启用/禁用点击音效
    /// </summary>
    public void SetEnableClickSound(bool enable)
    {
        m_EnableClickSound = enable;
    }

    /// <summary>
    /// 设置点击音效路径
    /// </summary>
    public void SetClickSoundPath(string path)
    {
        m_ClickSoundPath = path;
    }

    /// <summary>
    /// 启用/禁用防连点
    /// </summary>
    public void SetEnableClickInterval(bool enable)
    {
        m_EnableClickInterval = enable;
    }

    /// <summary>
    /// 设置点击间隔
    /// </summary>
    public void SetClickInterval(float interval)
    {
        m_ClickInterval = interval;
    }

    /// <summary>
    /// 启用/禁用点击动画
    /// </summary>
    public void SetEnableClickAnimation(bool enable)
    {
        m_EnableClickAnimation = enable;
    }

    /// <summary>
    /// 设置动画类型
    /// </summary>
    public void SetAnimationType(ClickAnimationType type)
    {
        m_AnimationType = type;
    }

    /// <summary>
    /// 启用/禁用长按
    /// </summary>
    public void SetEnableLongPress(bool enable)
    {
        m_EnableLongPress = enable;
    }

    /// <summary>
    /// 模拟点击
    /// </summary>
    public void SimulateClick()
    {
        if (interactable)
        {
            onClick?.Invoke();
        }
    }

    #endregion
}

/// <summary>
/// 点击动画类型
/// </summary>
public enum ClickAnimationType
{
    None,   // 无动画
    Scale,  // 缩放动画
    Punch   // 弹跳动画
}
