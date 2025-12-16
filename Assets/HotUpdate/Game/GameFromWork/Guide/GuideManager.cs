using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 新手引导管理器
/// 核心类，负责引导流程控制
/// </summary>
/// 启动引导: CheckAndStartGuide
//     ↓
// 加载配置 + 恢复进度
//     ↓
// ExecuteCurrentStep (保存进度)
//     ↓
// 检查触发条件
//     ↓
// 切换场景 (关闭旧窗口 + 打开新窗口)
//     ↓
// 执行步骤
//     ↓
// NextStep (再次保存进度)
//     ↓
// 循环或完成
//     ↓
// CleanupGuide (关闭所有窗口)
public class GuideManager : GameSingleton<GuideManager>
{
    #region 字段

    /// <summary>
    ///引导开关
    /// </summary>
    public static bool IsGuideOpen = false;

    /// <summary>
    /// 当前引导组ID
    /// </summary>
    private int _currentGroupId = -1;

    /// <summary>
    /// 当前引导步骤索引
    /// </summary>
    private int _currentStepIndex = 0;

    /// <summary>
    /// 当前引导步骤列表
    /// </summary>
    private List<GuideStepConfig> _currentSteps = new List<GuideStepConfig>();

    /// <summary>
    /// 当前引导步骤
    /// </summary>
    private GuideStepConfig _currentStep = null;

    /// <summary>
    /// 是否正在引导中
    /// </summary>
    private bool _isGuiding = false;

    /// <summary>
    /// 是否暂停引导
    /// </summary>
    private bool _isPaused = false;

    /// <summary>
    /// 三种引导UI窗口
    /// </summary>
    private WorldGuideView _worldGuideView;
    private UIGuideView _uiGuideView;
    private DialogGuideView _dialogGuideView;

    /// <summary>
    /// 当前使用的场景类型
    /// </summary>
    private GuideSceneType _currentSceneType = GuideSceneType.UI;

    /// <summary>
    /// 引导步骤执行器映射
    /// </summary>
    private Dictionary<GuideType, IGuideStepExecutor> _executors = new Dictionary<GuideType, IGuideStepExecutor>();

    /// <summary>
    /// 自定义引导处理器
    /// </summary>
    private Dictionary<string, Action<GuideStepConfig>> _customHandlers = new Dictionary<string, Action<GuideStepConfig>>();

    #endregion

    #region 属性

    /// <summary>
    /// 是否正在引导中
    /// </summary>
    public bool IsGuiding => _isGuiding;

    /// <summary>
    /// 当前引导组ID
    /// </summary>
    public int CurrentGroupId => _currentGroupId;

    /// <summary>
    /// 当前步骤索引
    /// </summary>
    public int CurrentStepIndex => _currentStepIndex;

    /// <summary>
    /// 当前步骤配置
    /// </summary>
    public GuideStepConfig CurrentStep => _currentStep;

    #endregion

    #region 初始化

    public override void Init()
    {
        base.Init();
        
        // 初始化执行器
        InitExecutors();

        LogUtlis.Info("[GuideManager] 新手引导管理器初始化完成");
    }

    /// <summary>
    /// 初始化执行器
    /// </summary>
    private void InitExecutors()
    {
        _executors.Clear();
        _executors[GuideType.Click] = new GuideClickExecutor();
        _executors[GuideType.Dialog] = new GuideDialogExecutor();
        _executors[GuideType.ForceWait] = new GuideForceWaitExecutor();
        _executors[GuideType.Highlight] = new GuideHighlightExecutor();
        _executors[GuideType.Custom] = new GuideCustomExecutor();
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 检查并启动引导
    /// 如果引导已完成或正在进行中，则跳过
    /// </summary>
    /// <param name="groupId">引导组ID</param>
    public void CheckAndStartGuide(int groupId)
    {

#if UNITY_EDITOR
        // 开发模式：跳过引导
        if (IsGuideOpen)
        {
            LogUtlis.Info($"[GuideManager] 开发模式已开启，跳过引导组{groupId}");
            return;
        }
#endif
        if (_isGuiding)
        {
            LogUtlis.Warn($"[GuideManager] 已有引导正在进行中: {_currentGroupId}");
            return;
        }

        // 从GuideData检查引导是否已完成
        if (GuideData.Instance.IsGuideCompleted(groupId))
        {
            LogUtlis.Info($"[GuideManager] 引导组{groupId}已完成，跳过");
            return;
        }

        // 从GuideData加载引导步骤配置
        _currentSteps = GuideData.Instance.LoadGuideConfig(groupId);
        if (_currentSteps == null || _currentSteps.Count == 0)
        {
            LogUtlis.Error($"[GuideManager] 未找到引导组{groupId}的配置");
            return;
        }

        _currentGroupId = groupId;
        _isGuiding = true;
        _isPaused = false;

        // 获取上次保存的进度（支持断点续传）
        int savedProgress = GuideData.Instance.GetGuideProgress(groupId);
        _currentStepIndex = savedProgress > 0 ? savedProgress : 0;

        // 触发引导开始事件
        EngineEventManager.Instance.DispatchEvent(new EngineEvent((int)GuideEventType.GuideStart, groupId));

        if (savedProgress > 0)
        {
            LogUtlis.Info($"[GuideManager] 继续引导组{groupId}，从第{_currentStepIndex + 1}步开始，共{_currentSteps.Count}步");
        }
        else
        {
            LogUtlis.Info($"[GuideManager] 开始引导组{groupId}，共{_currentSteps.Count}步");
        }

        // 执行当前步骤，可能是第一步，也可能是断点续传
        ExecuteCurrentStep();
    }

    /// <summary>
    /// 下一步
    /// </summary>
    public void NextStep()
    {
        if (!_isGuiding || _isPaused)
        {
            return;
        }

        // 触发步骤结束事件
        if (_currentStep != null)
        {
            EngineEventManager.Instance.DispatchEvent(new EngineEvent((int)GuideEventType.GuideStepEnd, _currentStep.Id));
        }

        _currentStepIndex++;

        // 保存进度（每步都保存，支持断点续传）
        if (_currentStepIndex < _currentSteps.Count)
        {
            ReportGuideProgress();
        }

        if (_currentStepIndex >= _currentSteps.Count)
        {
            // 引导完成
            CompleteGuide();
        }
        else
        {
            // 执行下一步
            ExecuteCurrentStep();
        }
    }

    /// <summary>
    /// 暂停引导
    /// </summary>
    public void PauseGuide()
    {
        if (!_isGuiding || _isPaused)
        {
            return;
        }

        _isPaused = true;
        HideCurrentGuideUI();
        EngineEventManager.Instance.DispatchEvent(new EngineEvent((int)GuideEventType.GuidePause, _currentGroupId));
        LogUtlis.Info($"[GuideManager] 暂停引导组{_currentGroupId}");
    }

    /// <summary>
    /// 恢复引导
    /// </summary>
    public void ResumeGuide()
    {
        if (!_isGuiding || !_isPaused)
        {
            return;
        }

        _isPaused = false;
        ShowCurrentGuideUI();
        EngineEventManager.Instance.DispatchEvent(new EngineEvent((int)GuideEventType.GuideResume, _currentGroupId));
        LogUtlis.Info($"[GuideManager] 恢复引导组{_currentGroupId}");
    }

    /// <summary>
    /// 跳过引导
    /// </summary>
    public void SkipGuide()
    {
        if (!_isGuiding)
        {
            return;
        }

        LogUtlis.Info($"[GuideManager] 跳过引导组{_currentGroupId}");
        EngineEventManager.Instance.DispatchEvent(new EngineEvent((int)GuideEventType.GuideSkip, _currentGroupId));
        
        // 标记为已完成
        GuideData.Instance.MarkGuideCompleted(_currentGroupId);
        GuideData.Instance.ReportGuideCompleteToServer(_currentGroupId);

        // 清理
        CleanupGuide();
    }

    /// <summary>
    /// 完成引导
    /// </summary>
    private void CompleteGuide()
    {
        LogUtlis.Info($"[GuideManager] 完成引导组{_currentGroupId}");
        
        // 触发完成事件
        EngineEventManager.Instance.DispatchEvent(new EngineEvent((int)GuideEventType.GuideComplete, _currentGroupId));

        // 标记为已完成
        GuideData.Instance.MarkGuideCompleted(_currentGroupId);
        GuideData.Instance.ReportGuideCompleteToServer(_currentGroupId);

        // 清理
        CleanupGuide();
    }

    /// <summary>
    /// 重置引导（清除完成记录）
    /// </summary>
    public void ResetGuide(int groupId)
    {
        GuideData.Instance.ClearGuideProgress(groupId);
        LogUtlis.Info($"[GuideManager] 重置引导组{groupId}");
    }

    /// <summary>
    /// 重置所有引导
    /// </summary>
    public void ResetAllGuides()
    {
        GuideData.Instance.ClearAllGuideProgress();
        LogUtlis.Info("[GuideManager] 重置所有引导");
    }

    /// <summary>    /// 检查引导是否已完成
    /// </summary>
    public bool IsGuideCompleted(int groupId)
    {
        return GuideData.Instance.IsGuideCompleted(groupId);
    }

    /// <summary>    /// 注册自定义引导处理器
    /// </summary>
    public void RegisterCustomHandler(string handlerName, Action<GuideStepConfig> handler)
    {
        _customHandlers[handlerName] = handler;
        LogUtlis.Info($"[GuideManager] 注册自定义处理器: {handlerName}");
    }

    /// <summary>
    /// 获取自定义处理器
    /// </summary>
    public Action<GuideStepConfig> GetCustomHandler(string handlerName)
    {
        return _customHandlers.ContainsKey(handlerName) ? _customHandlers[handlerName] : null;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 执行当前步骤
    /// </summary>
    private void ExecuteCurrentStep()
    {
        if (_currentStepIndex >= _currentSteps.Count)
        {
            return;
        }

        _currentStep = _currentSteps[_currentStepIndex];

        LogUtlis.Info($"[GuideManager] 执行步骤 {_currentStepIndex + 1}/{_currentSteps.Count}: {_currentStep.GuideType}, SceneType={_currentStep.SceneType}, Target={_currentStep.TargetPath}");

        // 保存当前进度（步骤开始时保存，支持断点续传）
        ReportGuideProgress();

        // 触发步骤开始事件
        EngineEventManager.Instance.DispatchEvent(new EngineEvent((int)GuideEventType.GuideStepStart, _currentStep.Id));

        // 检查触发条件
        if (!CheckTriggerCondition(_currentStep))
        {
            LogUtlis.Warn($"[GuideManager] 步骤{_currentStep.Id}触发条件不满足，跳过");
            NextStep();
            return;
        }

        // 切换引导场景类型（会关闭旧窗口）
        SwitchGuideScene(_currentStep.SceneType);

        // 获取当前场景的UI控制器
        object currentUI = GetCurrentGuideUIController();

        // 执行步骤
        if (_executors.ContainsKey(_currentStep.GuideType))
        {
            _executors[_currentStep.GuideType].Execute(_currentStep, this, currentUI);
        }
        else
        {
            LogUtlis.Error($"[GuideManager] 未找到引导类型{_currentStep.GuideType}的执行器");
            NextStep();
        }
    }

    /// <summary>
    /// 报告引导进度到服务器
    /// </summary>
    private void ReportGuideProgress()
    {
        // 只保存本地步骤进度，不上报服务器
        // 服务器只关心引导组是否完成
        GuideData.Instance.SaveGuideProgress(_currentGroupId, _currentStepIndex);
    }

    /// <summary>
    /// 检查触发条件
    /// TriggerCondition格式: "conditionId1:param1,conditionId2:param2"
    /// 例如: "1001:5,1002:100" 表示需要同时满足条件1001(参数5)和条件1002(参数100)
    /// </summary>
    private bool CheckTriggerCondition(GuideStepConfig step)
    {
        if (string.IsNullOrEmpty(step.TriggerCondition))
        {
            return true;
        }

        // 解析条件字符串
        string[] conditionPairs = step.TriggerCondition.Split(',');
        
        foreach (string pair in conditionPairs)
        {
            if (string.IsNullOrEmpty(pair.Trim()))
                continue;
            
            // 解析 "conditionId:param" 格式
            string[] parts = pair.Split(':');
            if (parts.Length == 0)
                continue;
            
            // 获取条件ID
            if (!int.TryParse(parts[0].Trim(), out int conditionId))
            {
                LogUtlis.Warn($"[GuideManager] 无效的条件ID格式: {parts[0]}");
                continue;
            }
            
            // 参数部分（可选）
            string param = parts.Length > 1 ? parts[1].Trim() : "";
            
            // 调用条件系统检查
            // 注意：这里假设配置表中的条件已经正确配置了参数
            // 如果需要传递参数，需要扩展ConditionManager的API
            bool conditionMet = ConditionManager.Instance.CheckCondition(conditionId);
            
            if (!conditionMet)
            {
                LogUtlis.Info($"[GuideManager] 引导触发条件不满足: 条件ID={conditionId}, 参数={param}");
                return false;
            }
        }
        
        LogUtlis.Info($"[GuideManager] 引导触发条件全部满足: {step.TriggerCondition}");
        return true;
    }

    /// <summary>
    /// 切换引导场景
    /// </summary>
    private void SwitchGuideScene(GuideSceneType sceneType)
    {
        // 如果场景类型未变化，不需要切换
        if (_currentSceneType == sceneType)
        {
            return;
        }

        // 关闭当前场景的引导窗口（而非Hide，避免窗口累积）
        CloseCurrentGuideWindow();

        // 切换到新场景
        _currentSceneType = sceneType;

        // 显示新场景的引导UI
        ShowCurrentGuideUI();

        LogUtlis.Info($"[GuideManager] 切换引导场景: {sceneType}");
    }

    /// <summary>
    /// 显示当前场景的引导UI
    /// </summary>
    private void ShowCurrentGuideUI()
    {
        switch (_currentSceneType)
        {
            case GuideSceneType.World:
                if (_worldGuideView == null)
                {
                    _worldGuideView = UIManager.Instance.OpenWindow<WorldGuideView>("WorldGuideView");
                }
                _worldGuideView?.Show();
                break;

            case GuideSceneType.UI:
                if (_uiGuideView == null)
                {
                    _uiGuideView = UIManager.Instance.OpenWindow<UIGuideView>("UIGuideView");
                }
                _uiGuideView?.Show();
                break;

            case GuideSceneType.Dialog:
                if (_dialogGuideView == null)
                {
                    _dialogGuideView = UIManager.Instance.OpenWindow<DialogGuideView>("DialogGuideView");
                }
                _dialogGuideView?.Show();
                break;
        }
    }

    /// <summary>    /// 关闭当前场景的引导窗口
    /// </summary>
    private void CloseCurrentGuideWindow()
    {
        switch (_currentSceneType)
        {
            case GuideSceneType.World:
                if (_worldGuideView != null)
                {
                    UIManager.Instance.CloseWindow("WorldGuideView");
                    _worldGuideView = null;
                }
                break;

            case GuideSceneType.UI:
                if (_uiGuideView != null)
                {
                    UIManager.Instance.CloseWindow("UIGuideView");
                    _uiGuideView = null;
                }
                break;

            case GuideSceneType.Dialog:
                if (_dialogGuideView != null)
                {
                    UIManager.Instance.CloseWindow("DialogGuideView");
                    _dialogGuideView = null;
                }
                break;
        }
    }

    /// <summary>    /// 隐藏当前场景的引导UI
    /// </summary>
    private void HideCurrentGuideUI()
    {
        switch (_currentSceneType)
        {
            case GuideSceneType.World:
                _worldGuideView?.Hide();
                break;

            case GuideSceneType.UI:
                _uiGuideView?.Hide();
                break;

            case GuideSceneType.Dialog:
                _dialogGuideView?.Hide();
                break;
        }
    }

    /// <summary>
    /// 获取当前场景的UI控制器
    /// </summary>
    private object GetCurrentGuideUIController()
    {
        switch (_currentSceneType)
        {
            case GuideSceneType.World:
                return _worldGuideView;

            case GuideSceneType.UI:
                return _uiGuideView;

            case GuideSceneType.Dialog:
                return _dialogGuideView;

            default:
                return null;
        }
    }

    /// <summary>
    /// 清理引导
    /// </summary>
    private void CleanupGuide()
    {
        _isGuiding = false;
        _isPaused = false;
        _currentGroupId = -1;
        _currentStepIndex = 0;
        _currentStep = null;
        _currentSteps.Clear();

        // 关闭所有引导UI窗口
        if (_worldGuideView != null)
        {
            UIManager.Instance.CloseWindow("WorldGuideView");
            _worldGuideView = null;
        }

        if (_uiGuideView != null)
        {
            UIManager.Instance.CloseWindow("UIGuideView");
            _uiGuideView = null;
        }

        if (_dialogGuideView != null)
        {
            UIManager.Instance.CloseWindow("DialogGuideView");
            _dialogGuideView = null;
        }
    }

    /// <summary>
    /// 获取测试引导步骤（临时）
    /// </summary>
    private List<GuideStepConfig> GetTestGuideSteps(int groupId)
    {
        var steps = new List<GuideStepConfig>();

        if (groupId == 1) // 新手引导第一组
        {
            steps.Add(new GuideStepConfig
            {
                Id = 1001,
                GroupId = 1,
                StepOrder = 1,
                GuideType = GuideType.Dialog,
                TipText = "欢迎来到游戏！让我来教你如何开始冒险。",
                AutoNext = false,
                WaitTime = 0
            });

            steps.Add(new GuideStepConfig
            {
                Id = 1002,
                GroupId = 1,
                StepOrder = 2,
                GuideType = GuideType.Click,
                TargetPath = "MainView/BagBtn",
                HighlightType = HighlightType.Circle,
                ArrowDirection = ArrowDirection.Up,
                TipText = "点击背包按钮打开背包",
                AutoNext = false
            });
        }

        return steps;
    }

    #endregion

    #region 销毁

    public override void OnDestroy()
    {
        base.OnDestroy();
        CleanupGuide();
        _executors.Clear();
        _customHandlers.Clear();
    }

    #endregion
}
