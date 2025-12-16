using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 引导步骤执行器接口
/// </summary>
public interface IGuideStepExecutor
{
    void Execute(GuideStepConfig step, GuideManager manager, object uiController);
}

/// <summary>
/// 点击引导执行器
/// 支持UI窗口和大世界场景
/// </summary>
public class GuideClickExecutor : IGuideStepExecutor
{
    public void Execute(GuideStepConfig step, GuideManager manager, object uiController)
    {
        LogUtlis.Info($"[GuideClickExecutor] 执行点击引导: SceneType={step.SceneType}, Target={step.TargetPath}");

        bool isStrongGuide = step.ForceType == GuideForceType.Strong;

        // 根据场景类型执行不同逻辑
        switch (step.SceneType)
        {
            case GuideSceneType.UI:
                ExecuteUIClick(step, manager, uiController as UIGuideView, isStrongGuide);
                break;

            case GuideSceneType.World:
                ExecuteWorldClick(step, manager, uiController as WorldGuideView, isStrongGuide);
                break;

            default:
                LogUtlis.Error($"[GuideClickExecutor] 不支持的场景类型: {step.SceneType}");
                manager.NextStep();
                break;
        }
    }

    /// <summary>
    /// 执行UI窗口点击引导
    /// </summary>
    private void ExecuteUIClick(GuideStepConfig step, GuideManager manager, UIGuideView uiView, bool isStrongGuide)
    {
        if (uiView == null)
        {
            LogUtlis.Error("[GuideClickExecutor] UIGuideView为null");
            manager.NextStep();
            return;
        }

        // 查找目标UI
        RectTransform target = FindUITarget(step.TargetPath);
        if (target == null)
        {
            LogUtlis.Error($"[GuideClickExecutor] 未找到目标UI: {step.TargetPath}");
            manager.NextStep();
            return;
        }

        // 显示高亮和提示
        uiView.HighlightUIElement(target, step.HighlightType, isStrongGuide);
        uiView.ShowArrow(target, step.ArrowDirection);
        
        if (!string.IsNullOrEmpty(step.TipText))
        {
            uiView.ShowTip(step.TipText, target);
        }

        // 添加点击监听
        uiView.SetClickTarget(target, () =>
        {
            LogUtlis.Info($"[GuideClickExecutor] 用户点击了目标");
            uiView.HideAll();
            
            if (step.AutoNext)
            {
                manager.NextStep();
            }
        });
    }

    /// <summary>
    /// 执行大世界点击引导
    /// </summary>
    private void ExecuteWorldClick(GuideStepConfig step, GuideManager manager, WorldGuideView worldView, bool isStrongGuide)
    {
        if (worldView == null)
        {
            LogUtlis.Error("[GuideClickExecutor] WorldGuideView为null");
            manager.NextStep();
            return;
        }

        // 查找世界对象
        Transform target = FindWorldTarget(step.TargetPath);
        if (target == null)
        {
            LogUtlis.Error($"[GuideClickExecutor] 未找到世界对象: {step.TargetPath}");
            manager.NextStep();
            return;
        }

        // 高亮世界对象
        worldView.HighlightWorldObject(target, isStrongGuide);
        worldView.ShowArrow(target, step.ArrowDirection);
        worldView.ShowFinger(target);
        
        if (!string.IsNullOrEmpty(step.TipText))
        {
            worldView.ShowTip(step.TipText, target);
        }

        // 添加点击监听
        worldView.SetClickTarget(target, () =>
        {
            LogUtlis.Info($"[GuideClickExecutor] 用户点击了世界对象");
            worldView.HideAll();
            
            if (step.AutoNext)
            {
                manager.NextStep();
            }
        });
    }

    private RectTransform FindUITarget(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        string[] parts = path.Split('/');
        if (parts.Length == 0) return null;

        BaseUIView window = UIManager.Instance.GetOpenedWindow(parts[0]);
        if (window == null) return null;

        GameObject windowGo = window.ViewInstance;
        if (windowGo == null) return null;

        Transform trans = windowGo.transform;
        for (int i = 1; i < parts.Length; i++)
        {
            trans = trans.Find(parts[i]);
            if (trans == null) return null;
        }

        return trans as RectTransform;
    }

    private Transform FindWorldTarget(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // 通过GameObject.Find或Tag查找世界对象
        GameObject go = GameObject.Find(path);
        return go != null ? go.transform : null;
    }
}

/// <summary>
/// 对话引导执行器
/// 对话引导永远是强引导
/// </summary>
public class GuideDialogExecutor : IGuideStepExecutor
{
    public void Execute(GuideStepConfig step, GuideManager manager, object uiController)
    {
        LogUtlis.Info($"[GuideDialogExecutor] 显示对话: {step.TipText}");

        DialogGuideView dialogView = uiController as DialogGuideView;
        if (dialogView == null)
        {
            LogUtlis.Error("[GuideDialogExecutor] DialogGuideView为null");
            manager.NextStep();
            return;
        }

        // 显示对话（使用新接口，传入GuideStepConfig）
        bool isLastStep = step.Id == manager.CurrentStep.Id && manager.CurrentStepIndex == manager.CurrentStep.Id;
        dialogView.ShowDialog(
            step,  // 直接传入GuideStepConfig，包含所有资源配置
            () =>
            {
                LogUtlis.Info($"[GuideDialogExecutor] 用户确认对话");
                dialogView.HideAll();
                
                if (step.AutoNext)
                {
                    manager.NextStep();
                }
            },
            isLastStep
        );
    }
}

/// <summary>
/// 强制等待执行器
/// 用于引导中的强制等待时间
/// </summary>
public class GuideForceWaitExecutor : IGuideStepExecutor
{
    public void Execute(GuideStepConfig step, GuideManager manager, object uiController)
    {
        LogUtlis.Info($"[GuideForceWaitExecutor] 强制等待: {step.WaitTime}秒");

        // 显示等待提示（如果有）
        if (!string.IsNullOrEmpty(step.TipText))
        {
            // 根据场景类型显示提示
            switch (step.SceneType)
            {
                case GuideSceneType.UI:
                    (uiController as UIGuideView)?.ShowTip(step.TipText, null);
                    break;
                case GuideSceneType.World:
                    (uiController as WorldGuideView)?.ShowTip(step.TipText, null);
                    break;
            }
        }

        // 延迟执行
        string timerName = $"GuideWait_{step.Id}";
        TimerManager.Instance.GetTimer(timerName, step.WaitTime, () =>
        {
            // 隐藏UI
            switch (step.SceneType)
            {
                case GuideSceneType.UI:
                    (uiController as UIGuideView)?.HideAll();
                    break;
                case GuideSceneType.World:
                    (uiController as WorldGuideView)?.HideAll();
                    break;
                case GuideSceneType.Dialog:
                    (uiController as DialogGuideView)?.HideAll();
                    break;
            }
            
            manager.NextStep();
        }, true);
    }
}

/// <summary>
/// 高亮引导执行器
/// 仅高亮显示，不阻止其他操作
/// </summary>
public class GuideHighlightExecutor : IGuideStepExecutor
{
    public void Execute(GuideStepConfig step, GuideManager manager, object uiController)
    {
        LogUtlis.Info($"[GuideHighlightExecutor] 高亮显示: SceneType={step.SceneType}, Target={step.TargetPath}");

        bool isStrongGuide = step.ForceType == GuideForceType.Strong;

        // 根据场景类型执行
        switch (step.SceneType)
        {
            case GuideSceneType.UI:
                ExecuteUIHighlight(step, manager, uiController as UIGuideView, isStrongGuide);
                break;

            case GuideSceneType.World:
                ExecuteWorldHighlight(step, manager, uiController as WorldGuideView, isStrongGuide);
                break;

            default:
                LogUtlis.Error($"[GuideHighlightExecutor] 不支持的场景类型: {step.SceneType}");
                manager.NextStep();
                break;
        }
    }

    private void ExecuteUIHighlight(GuideStepConfig step, GuideManager manager, UIGuideView uiView, bool isStrongGuide)
    {
        if (uiView == null)
        {
            manager.NextStep();
            return;
        }

        RectTransform target = FindUITarget(step.TargetPath);
        if (target == null)
        {
            LogUtlis.Error($"[GuideHighlightExecutor] 未找到UI目标: {step.TargetPath}");
            manager.NextStep();
            return;
        }

        uiView.HighlightUIElement(target, step.HighlightType, isStrongGuide);
        
        if (!string.IsNullOrEmpty(step.TipText))
        {
            uiView.ShowTip(step.TipText, target);
        }

        // 自动进入下一步
        if (step.AutoNext)
        {
            float waitTime = step.WaitTime > 0 ? step.WaitTime : 2f;
            string timerName = $"GuideHighlight_{step.Id}";
            TimerManager.Instance.GetTimer(timerName, waitTime, () =>
            {
                uiView.HideAll();
                manager.NextStep();
            }, true);
        }
    }

    private void ExecuteWorldHighlight(GuideStepConfig step, GuideManager manager, WorldGuideView worldView, bool isStrongGuide)
    {
        if (worldView == null)
        {
            manager.NextStep();
            return;
        }

        Transform target = FindWorldTarget(step.TargetPath);
        if (target == null)
        {
            LogUtlis.Error($"[GuideHighlightExecutor] 未找到世界目标: {step.TargetPath}");
            manager.NextStep();
            return;
        }

        worldView.HighlightWorldObject(target, isStrongGuide);
        
        if (!string.IsNullOrEmpty(step.TipText))
        {
            worldView.ShowTip(step.TipText, target);
        }

        // 自动进入下一步
        if (step.AutoNext)
        {
            float waitTime = step.WaitTime > 0 ? step.WaitTime : 2f;
            string timerName = $"GuideHighlight_{step.Id}";
            TimerManager.Instance.GetTimer(timerName, waitTime, () =>
            {
                worldView.HideAll();
                manager.NextStep();
            }, true);
        }
    }

    private RectTransform FindUITarget(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        string[] parts = path.Split('/');
        if (parts.Length == 0) return null;

        BaseUIView window = UIManager.Instance.GetOpenedWindow(parts[0]);
        if (window == null) return null;

        GameObject windowGo = window.ViewInstance;
        if (windowGo == null) return null;

        Transform trans = windowGo.transform;
        for (int i = 1; i < parts.Length; i++)
        {
            trans = trans.Find(parts[i]);
            if (trans == null) return null;
        }

        return trans as RectTransform;
    }

    private Transform FindWorldTarget(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        GameObject go = GameObject.Find(path);
        return go != null ? go.transform : null;
    }
}

/// <summary>
/// 自定义引导执行器
/// </summary>
public class GuideCustomExecutor : IGuideStepExecutor
{
    public void Execute(GuideStepConfig step, GuideManager manager, object uiController)
    {
        LogUtlis.Info($"[GuideCustomExecutor] 执行自定义引导: {step.CustomParam}");

        // 解析自定义参数
        if (string.IsNullOrEmpty(step.CustomParam))
        {
            LogUtlis.Warn($"[GuideCustomExecutor] 自定义参数为空");
            manager.NextStep();
            return;
        }

        // 查找自定义处理器
        var handler = manager.GetCustomHandler(step.CustomParam);
        if (handler != null)
        {
            handler.Invoke(step);
        }
        else
        {
            LogUtlis.Error($"[GuideCustomExecutor] 未找到自定义处理器: {step.CustomParam}");
            manager.NextStep();
        }
    }
}
