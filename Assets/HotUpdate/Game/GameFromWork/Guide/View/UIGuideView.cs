using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

/// <summary>
/// UI窗口引导窗口
/// 负责UI界面引导
/// </summary>
public class UIGuideView : BaseUIView
{
    [Header("配置")]
    private Color MaskColor = new Color(0, 0, 0, 0.5f);      // 弱引导遮罩
    private Color StrongMaskColor = new Color(0, 0, 0, 0.8f); // 强引导遮罩
    private float HighlightPadding = 30f;
    private float ArrowAnimDuration = 0.5f;
    private float ArrowAnimDistance = 20f;

    private RectTransform _currentTarget;
    private Action _clickCallback;
    private bool _isStrongGuide = false;
    private Tweener _arrowTweener;

    public override void OnCreate()
    {
        base.OnCreate();
        
        HideAll();
        
        // 遮罩默认不阻挡点击
        if (Img.ContainsKey("Mask"))
        {
            Img["Mask"].raycastTarget = false;
            
            // 添加点击监听
            if (Btn.ContainsKey("Mask"))
            {
                Btn["Mask"].onClick.AddListener(OnMaskClick);
            }
        }

        LogUtlis.Info("[UIGuideView] UI窗口引导初始化");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        
        if (_arrowTweener != null)
        {
            _arrowTweener.Kill();
            _arrowTweener = null;
        }

        _clickCallback = null;
        _currentTarget = null;
    }

    #region 显示/隐藏

    public void Show()
    {
        SetActive(true, ViewActiveFlag.LogicalFlag);
    }

    public void Hide()
    {
        SetActive(false, ViewActiveFlag.LogicalFlag);
    }

    public void HideAll()
    {
        HideMask();
        HideHighlight();
        HideArrow();
        HideTip();
        _currentTarget = null;
        _clickCallback = null;
    }

    #endregion

    #region 遮罩

    public void ShowMask(bool isStrongGuide)
    {
        _isStrongGuide = isStrongGuide;
        
        if (Img.ContainsKey("Mask"))
        {
            Img["Mask"].gameObject.SetActive(true);
            Img["Mask"].color = isStrongGuide ? StrongMaskColor : MaskColor;
            Img["Mask"].raycastTarget = isStrongGuide;
        }
    }

    public void HideMask()
    {
        if (Img.ContainsKey("Mask"))
        {
            Img["Mask"].gameObject.SetActive(false);
        }
    }

    #endregion

    #region 高亮UI元素

    public void HighlightUIElement(RectTransform target, HighlightType highlightType, bool isStrongGuide)
    {
        if (target == null) return;

        _currentTarget = target;
        _isStrongGuide = isStrongGuide;

        ShowMask(isStrongGuide);

        if (Obj.ContainsKey("Highlight"))
        {
            RectTransform highlightRect = Obj["Highlight"].GetComponent<RectTransform>();
            if (highlightRect != null)
            {
                highlightRect.gameObject.SetActive(true);
                highlightRect.position = target.position;
                highlightRect.sizeDelta = target.sizeDelta + Vector2.one * HighlightPadding;

                // 根据类型设置形状
                Image highlightImg = Obj["Highlight"].GetComponent<Image>();
                if (highlightImg != null)
                {
                    switch (highlightType)
                    {
                        case HighlightType.Circle:
                            highlightImg.type = Image.Type.Filled;
                            highlightImg.fillMethod = Image.FillMethod.Radial360;
                            break;
                        case HighlightType.Rect:
                            highlightImg.type = Image.Type.Simple;
                            break;
                    }
                }
            }
        }

        // 弱引导允许点击目标UI
        if (!isStrongGuide && target != null)
        {
            ButtonPro targetBtn = target.GetComponent<ButtonPro>();
            if (targetBtn != null)
            {
                targetBtn.interactable = true;
            }
        }
    }

    public void HideHighlight()
    {
        if (Obj.ContainsKey("Highlight"))
        {
            Obj["Highlight"].SetActive(false);
        }
    }

    #endregion

    #region 箭头

    public void ShowArrow(RectTransform target, ArrowDirection direction)
    {
        if (target == null || !Obj.ContainsKey("Arrow")) return;

        GameObject arrowObj = Obj["Arrow"];
        arrowObj.SetActive(true);

        RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
        if (arrowRect == null) return;

        Vector3 arrowRotation = Vector3.zero;
        Vector2 offset = Vector2.zero;

        switch (direction)
        {
            case ArrowDirection.Up:
                offset = new Vector2(0, -100);
                arrowRotation.z = 0;
                break;
            case ArrowDirection.Down:
                offset = new Vector2(0, 100);
                arrowRotation.z = 180;
                break;
            case ArrowDirection.Left:
                offset = new Vector2(100, 0);
                arrowRotation.z = 90;
                break;
            case ArrowDirection.Right:
                offset = new Vector2(-100, 0);
                arrowRotation.z = -90;
                break;
        }

        arrowRect.position = target.position;
        arrowRect.anchoredPosition += offset;
        arrowRect.eulerAngles = arrowRotation;

        PlayArrowAnimation(arrowRect, direction);
    }

    private void PlayArrowAnimation(RectTransform arrowRect, ArrowDirection direction)
    {
        if (arrowRect == null) return;

        if (_arrowTweener != null)
        {
            _arrowTweener.Kill();
        }

        Vector2 offset = Vector2.zero;
        switch (direction)
        {
            case ArrowDirection.Up:
                offset = new Vector2(0, -ArrowAnimDistance);
                break;
            case ArrowDirection.Down:
                offset = new Vector2(0, ArrowAnimDistance);
                break;
            case ArrowDirection.Left:
                offset = new Vector2(ArrowAnimDistance, 0);
                break;
            case ArrowDirection.Right:
                offset = new Vector2(-ArrowAnimDistance, 0);
                break;
        }

        Vector2 startPos = arrowRect.anchoredPosition;
        Vector2 endPos = startPos + offset;
        
        _arrowTweener = DOTween.To(
            () => arrowRect.anchoredPosition,
            x => arrowRect.anchoredPosition = x,
            endPos,
            ArrowAnimDuration
        )
        .SetLoops(-1, LoopType.Yoyo)
        .SetEase(Ease.InOutSine);
    }

    public void HideArrow()
    {
        if (Obj.ContainsKey("Arrow"))
        {
            Obj["Arrow"].SetActive(false);
        }

        if (_arrowTweener != null)
        {
            _arrowTweener.Kill();
            _arrowTweener = null;
        }
    }

    #endregion

    #region 提示

    public void ShowTip(string text, RectTransform target = null)
    {
        if (!Obj.ContainsKey("Tip") || !Tmp.ContainsKey("TipText")) return;

        Obj["Tip"].SetActive(true);
        Tmp["TipText"].text = text;

        if (target != null)
        {
            RectTransform tipRect = Obj["Tip"].GetComponent<RectTransform>();
            if (tipRect != null)
            {
                tipRect.position = target.position;
                tipRect.anchoredPosition += new Vector2(0, 150);
            }
        }
    }

    public void HideTip()
    {
        if (Obj.ContainsKey("Tip"))
        {
            Obj["Tip"].SetActive(false);
        }
    }

    #endregion

    #region 点击处理

    public void SetClickTarget(RectTransform target, Action callback)
    {
        _currentTarget = target;
        _clickCallback = callback;

        if (!_isStrongGuide && target != null)
        {
            // 弱引导：直接监听目标按钮
            ButtonPro targetBtn = target.GetComponent<ButtonPro>();
            if (targetBtn != null)
            {
                targetBtn.onClick.RemoveListener(OnTargetClick);
                targetBtn.onClick.AddListener(OnTargetClick);
            }
        }
    }

    private void OnMaskClick()
    {
        // 强引导时，点击遮罩无效
        if (_isStrongGuide) return;

        // 弱引导时，点击遮罩可以关闭引导
        TriggerClick();
    }

    private void OnTargetClick()
    {
        TriggerClick();
    }

    public void TriggerClick()
    {
        if (_clickCallback != null)
        {
            Action callback = _clickCallback;
            _clickCallback = null;
            callback.Invoke();
        }
    }

    #endregion
}
