using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

/// <summary>
/// 对话框引导窗口
/// 对话框引导永远是强引导
/// </summary>
/// Img["Mask"] - 遮罩
// Obj["DialogPanel"] - 对话框面板
// Img["Background"] - 背景图 
// Obj["Avatar"] - 角色容器 位置可调
// Img["Avatar"] - 角色立绘
// Tmp["SpeakerName"] - 角色名字文本
// Tmp["DialogText"] - 对话文本
// Tmp["NextBtnText"] - 按钮文本
// Btn["NextBtn"] - 下一步按钮
// Btn["DialogPanel"] - 对话框点击区域
public class DialogGuideView : BaseUIView
{
    [Header("配置")]
    private Color MaskColor = new Color(0, 0, 0, 0.9f);  // 强遮罩
    private float TypeWriterSpeed = 0.05f;                // 打字机速度
    private string DefaultNextText = "下一步";             // 默认按钮文本

    private string _fullText = "";
    private bool _isTyping = false;
    private Action _nextCallback;
    private Tweener _typeTweener;

    public override void OnCreate()
    {
        base.OnCreate();
        
        HideAll();

        // 遮罩永远阻止点击
        if (Img.ContainsKey("Mask"))
        {
            Img["Mask"].raycastTarget = true;
            Img["Mask"].color = MaskColor;
        }

        // 下一步按钮监听
        if (Btn.ContainsKey("NextBtn"))
        {
            Btn["NextBtn"].onClick.AddListener(OnNextButtonClick);
        }

        // 对话框点击监听（点击加速或跳过打字）
        if (Btn.ContainsKey("DialogPanel"))
        {
            Btn["DialogPanel"].onClick.AddListener(OnDialogClick);
        }

        LogUtlis.Info("[DialogGuideView] 对话框引导初始化");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        
        if (_typeTweener != null)
        {
            _typeTweener.Kill();
            _typeTweener = null;
        }

        _nextCallback = null;
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
        if (Img.ContainsKey("Mask"))
        {
            Img["Mask"].gameObject.SetActive(false);
        }

        if (Obj.ContainsKey("DialogPanel"))
        {
            Obj["DialogPanel"].SetActive(false);
        }

        _isTyping = false;
        _fullText = "";
        _nextCallback = null;

        if (_typeTweener != null)
        {
            _typeTweener.Kill();
            _typeTweener = null;
        }
    }

    #endregion

    #region 对话显示

    /// <summary>
    /// 显示对话（使用GuideStepConfig）
    /// </summary>
    public void ShowDialog(GuideStepConfig stepConfig, Action nextCallback, bool isLastStep = false)
    {
        if (stepConfig == null)
        {
            LogUtlis.Error("[DialogGuideView] stepConfig为空");
            return;
        }

        // 使用GuideResConfig中的资源字段
        string speakerName = stepConfig.CharacterName ?? "";
        string avatarPath = stepConfig.CharacterAvatar ?? "";
        string dialogText = stepConfig.TipText ?? "";
        string backgroundPath = stepConfig.BackgroundImage ?? "";
        CharacterPosition charPos = stepConfig.CharacterPos;

        _nextCallback = nextCallback;
        _fullText = dialogText;

        // 显示遮罩
        if (Img.ContainsKey("Mask"))
        {
            Img["Mask"].gameObject.SetActive(true);
        }

        // 显示对话框
        if (Obj.ContainsKey("DialogPanel"))
        {
            Obj["DialogPanel"].SetActive(true);
        }

        // 加载背景图（GuideResConfig新增）
        LoadBackground(backgroundPath);

        // 设置说话人名字
        if (Tmp.ContainsKey("SpeakerName"))
        {
            Tmp["SpeakerName"].text = speakerName;
        }

        // 设置角色位置（GuideResConfig新增）
        SetCharacterPosition(charPos);

        // 加载头像
        if (!string.IsNullOrEmpty(avatarPath))
        {
            LoadAvatar(avatarPath);
        }
        else if (Img.ContainsKey("Avatar"))
        {
            Img["Avatar"].gameObject.SetActive(false);
        }

        // 设置按钮文本
        if (Tmp.ContainsKey("NextBtnText"))
        {
            Tmp["NextBtnText"].text = isLastStep ? "完成" : DefaultNextText;
        }

        // 播放打字机效果
        PlayTypeWriter(dialogText);

        // 播放对话框入场动画
        PlayDialogEnterAnimation();

        LogUtlis.Info($"[DialogGuideView] 显示对话: 角色={speakerName}, 位置={charPos}, 背景={backgroundPath}");
    }

    /// <summary>
    /// 显示对话
    /// </summary>
    public void ShowDialog(string speakerName, string avatarPath, string dialogText, Action nextCallback, bool isLastStep = false)
    {
        _nextCallback = nextCallback;
        _fullText = dialogText;

        // 显示遮罩
        if (Img.ContainsKey("Mask"))
        {
            Img["Mask"].gameObject.SetActive(true);
        }

        // 显示对话框
        if (Obj.ContainsKey("DialogPanel"))
        {
            Obj["DialogPanel"].SetActive(true);
        }

        // 设置说话人名字
        if (Tmp.ContainsKey("SpeakerName"))
        {
            Tmp["SpeakerName"].text = speakerName;
        }

        // 加载头像
        if (Img.ContainsKey("Avatar") && !string.IsNullOrEmpty(avatarPath))
        {
            LoadAvatar(avatarPath);
        }

        // 设置按钮文本
        if (Tmp.ContainsKey("NextBtnText"))
        {
            Tmp["NextBtnText"].text = isLastStep ? "完成" : DefaultNextText;
        }

        // 播放打字机效果
        PlayTypeWriter(dialogText);

        // 播放对话框入场动画
        PlayDialogEnterAnimation();
    }

    /// <summary>
    /// 加载头像
    /// </summary>
    private async void LoadAvatar(string avatarPath)
    {
        if (!Img.ContainsKey("Avatar")) return;

        var sprite = await LoadManager.Instance.LoadAssetAsync<Sprite>(avatarPath);
        if (sprite != null && Img.ContainsKey("Avatar"))
        {
            Img["Avatar"].sprite = sprite;
            Img["Avatar"].gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 加载背景图
    /// </summary>
    private async void LoadBackground(string backgroundPath)
    {
        if (string.IsNullOrEmpty(backgroundPath) || !Img.ContainsKey("Background")) 
        {
            // 没有背景图则隐藏
            if (Img.ContainsKey("Background"))
            {
                Img["Background"].gameObject.SetActive(false);
            }
            return;
        }

        var sprite = await LoadManager.Instance.LoadAssetAsync<Sprite>(backgroundPath);
        if (sprite != null && Img.ContainsKey("Background"))
        {
            Img["Background"].sprite = sprite;
            Img["Background"].gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 设置角色位置
    /// </summary>
    private void SetCharacterPosition(CharacterPosition position)
    {
        if (!Obj.ContainsKey("Avatar")) return;

        RectTransform avatarRect = Obj["Avatar"].GetComponent<RectTransform>();
        if (avatarRect == null) return;

        switch (position)
        {
            case CharacterPosition.Left:
                avatarRect.anchorMin = new Vector2(0, 0.5f);
                avatarRect.anchorMax = new Vector2(0, 0.5f);
                avatarRect.anchoredPosition = new Vector2(100, 0);
                break;

            case CharacterPosition.Right:
                avatarRect.anchorMin = new Vector2(1, 0.5f);
                avatarRect.anchorMax = new Vector2(1, 0.5f);
                avatarRect.anchoredPosition = new Vector2(-100, 0);
                break;

            case CharacterPosition.Center:
                avatarRect.anchorMin = new Vector2(0.5f, 0.5f);
                avatarRect.anchorMax = new Vector2(0.5f, 0.5f);
                avatarRect.anchoredPosition = Vector2.zero;
                break;

            case CharacterPosition.None:
            default:
                // 隐藏角色
                if (Img.ContainsKey("Avatar"))
                {
                    Img["Avatar"].gameObject.SetActive(false);
                }
                break;
        }
    }

    /// <summary>
    /// 打字机效果
    /// </summary>
    private void PlayTypeWriter(string text)
    {
        if (!Tmp.ContainsKey("DialogText")) return;

        _isTyping = true;
        Tmp["DialogText"].text = "";

        if (_typeTweener != null)
        {
            _typeTweener.Kill();
        }

        int currentIndex = 0;
        _typeTweener = DOTween.To(
            () => currentIndex,
            x => {
                currentIndex = x;
                if (Tmp.ContainsKey("DialogText"))
                {
                    Tmp["DialogText"].text = text.Substring(0, Mathf.Min(currentIndex, text.Length));
                }
            },
            text.Length,
            text.Length * TypeWriterSpeed
        )
        .SetEase(Ease.Linear)
        .OnComplete(() => {
            _isTyping = false;
            if (Tmp.ContainsKey("DialogText"))
            {
                Tmp["DialogText"].text = text;
            }
        });
    }

    /// <summary>
    /// 跳过打字机效果
    /// </summary>
    private void SkipTypeWriter()
    {
        if (_typeTweener != null)
        {
            _typeTweener.Kill();
            _typeTweener = null;
        }

        if (Tmp.ContainsKey("DialogText"))
        {
            Tmp["DialogText"].text = _fullText;
        }

        _isTyping = false;
    }

    /// <summary>
    /// 对话框入场动画
    /// </summary>
    private void PlayDialogEnterAnimation()
    {
        if (!Obj.ContainsKey("DialogPanel")) return;

        RectTransform dialogRect = Obj["DialogPanel"].GetComponent<RectTransform>();
        if (dialogRect != null)
        {
            dialogRect.localScale = new Vector3(0.8f, 0.8f, 1f);
            dialogRect.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    #endregion

    #region 点击处理

    private void OnDialogClick()
    {
        if (_isTyping)
        {
            // 正在打字，点击跳过
            SkipTypeWriter();
        }
    }

    private void OnNextButtonClick()
    {
        if (_isTyping)
        {
            // 正在打字，先跳过
            SkipTypeWriter();
            return;
        }

        // 触发下一步
        if (_nextCallback != null)
        {
            Action callback = _nextCallback;
            _nextCallback = null;
            callback.Invoke();
        }
    }

    #endregion
}
