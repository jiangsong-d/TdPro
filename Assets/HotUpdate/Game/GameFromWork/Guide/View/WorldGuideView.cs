using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

/// <summary>
/// 大世界引导窗口
/// 负责大世界场景中的引导显示
/// </summary>
public class WorldGuideView : BaseUIView
{
    [Header("配置")]
    private Color MaskColor = new Color(0, 0, 0, 0.5f);      // 弱引导遮罩
    private Color StrongMaskColor = new Color(0, 0, 0, 0.8f); // 强引导遮罩
    private float HighlightPadding = 30f;
    private float ArrowAnimDuration = 0.5f;
    private float ArrowAnimDistance = 20f;

    private Transform _currentTarget;
    private Action _clickCallback;
    private bool _isStrongGuide = false;
    private Tweener _arrowTweener;
    private Tweener _fingerTweener;

    public override void OnCreate()
    {
        base.OnCreate();
        
        HideAll();
        
        // 遮罩默认不阻挡点击
        if (Img.ContainsKey("Mask"))
        {
            Img["Mask"].raycastTarget = false;
        }

        LogUtlis.Info("[WorldGuideView] 大世界引导窗口初始化");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        
        if (_arrowTweener != null)
        {
            _arrowTweener.Kill();
            _arrowTweener = null;
        }

        if (_fingerTweener != null)
        {
            _fingerTweener.Kill();
            _fingerTweener = null;
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
        HideFinger();
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

    #region 高亮世界对象

    public void HighlightWorldObject(Transform target, bool isStrongGuide)
    {
        if (target == null) return;

        _currentTarget = target;
        _isStrongGuide = isStrongGuide;

        ShowMask(isStrongGuide);

        // 世界坐标转屏幕坐标
        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);
        
        if (Obj.ContainsKey("Highlight"))
        {
            RectTransform highlightRect = Obj["Highlight"].GetComponent<RectTransform>();
            if (highlightRect != null)
            {
                highlightRect.gameObject.SetActive(true);
                
                RectTransform canvasRect = ViewInstance.GetComponent<RectTransform>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPos,
                    ViewCanvas.worldCamera,
                    out Vector2 localPos
                );
                
                highlightRect.anchoredPosition = localPos;
                highlightRect.sizeDelta = new Vector2(200, 200);
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

    public void ShowArrow(Transform target, ArrowDirection direction)
    {
        if (target == null || !Obj.ContainsKey("Arrow")) return;

        GameObject arrowObj = Obj["Arrow"];
        arrowObj.SetActive(true);

        RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
        if (arrowRect == null) return;

        // 世界坐标转屏幕坐标
        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);
        
        RectTransform canvasRect = ViewInstance.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            ViewCanvas.worldCamera,
            out Vector2 localPos
        );

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

        arrowRect.anchoredPosition = localPos + offset;
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

    public void ShowTip(string text, Transform target)
    {
        if (!Obj.ContainsKey("Tip") || !Tmp.ContainsKey("TipText")) return;

        Obj["Tip"].SetActive(true);
        Tmp["TipText"].text = text;

        if (target != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);
            
            RectTransform tipRect = Obj["Tip"].GetComponent<RectTransform>();
            RectTransform canvasRect = ViewInstance.GetComponent<RectTransform>();
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                ViewCanvas.worldCamera,
                out Vector2 localPos
            );
            
            tipRect.anchoredPosition = localPos + new Vector2(0, 150);
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

    #region 手指引导

    public void ShowFinger(Transform target)
    {
        if (!Obj.ContainsKey("Finger") || target == null) return;

        Obj["Finger"].SetActive(true);

        RectTransform fingerRect = Obj["Finger"].GetComponent<RectTransform>();
        if (fingerRect != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);
            
            RectTransform canvasRect = ViewInstance.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                ViewCanvas.worldCamera,
                out Vector2 localPos
            );

            fingerRect.anchoredPosition = localPos;
            PlayFingerAnimation(fingerRect);
        }
    }

    private void PlayFingerAnimation(RectTransform fingerRect)
    {
        if (_fingerTweener != null)
        {
            _fingerTweener.Kill();
        }

        fingerRect.localScale = Vector3.one;
        _fingerTweener = fingerRect.DOScale(0.8f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void HideFinger()
    {
        if (Obj.ContainsKey("Finger"))
        {
            Obj["Finger"].SetActive(false);
        }

        if (_fingerTweener != null)
        {
            _fingerTweener.Kill();
            _fingerTweener = null;
        }
    }

    #endregion

    #region 点击处理

    public void SetClickTarget(Transform target, Action callback)
    {
        _currentTarget = target;
        _clickCallback = callback;
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
