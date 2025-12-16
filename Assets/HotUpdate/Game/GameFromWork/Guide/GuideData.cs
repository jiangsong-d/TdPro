using System.Collections.Generic;
using UnityEngine;
using cfg.Guide;

/// <summary>
/// 引导数据管理器
/// 职责：
/// 1. 处理服务器路引导数据
/// 2. 本地引导数据管理
/// 3. 引导配置表加载（集成Luban）
/// 4. 组合引导数据给GuideManager使用
/// </summary>
public class GuideData : GameSingleton<GuideData>
{
    #region 数据存储

    /// <summary>
    /// 服务器引导完成数据（服务器推送）
    /// Key: 引导组ID, Value: 完成状态
    /// 服务器只保存已完成的引导组，不保存步骤进度
    /// </summary>
    private Dictionary<int, bool> _serverGuideData = new Dictionary<int, bool>();

    /// <summary>
    /// 本地引导完成数据（本地缓存）
    /// Key: 引导组ID, Value: 完成状态
    /// 与服务器数据同步，但在未联网时也能本地记录
    /// </summary>
    private Dictionary<int, bool> _localGuideData = new Dictionary<int, bool>();

    /// <summary>
    /// 引导配置表缓存
    /// Key: 引导组ID, Value: 引导步骤列表
    /// </summary>
    private Dictionary<int, List<GuideStepConfig>> _guideConfigCache = new Dictionary<int, List<GuideStepConfig>>();

    /// <summary>
    /// 引导组内的步骤进度（仅本地保存）
    /// Key: 引导组ID, Value: 当前步骤索引
    /// 用于支持断点续传，服务器不关心步骤级别的进度
    /// </summary>
    private Dictionary<int, int> _guideProgress = new Dictionary<int, int>();

    /// <summary>
    /// 引导资源配置缓存
    /// Key: 资源配置ID, Value: 资源配置
    /// 用于缓存GuideResConfig，避免重复加载
    /// </summary>
    private Dictionary<int, cfg.Guide.GuideResConfig> _guideResConfigCache = new Dictionary<int, cfg.Guide.GuideResConfig>();

    #endregion

    #region 初始化

    public override void Init()
    {
        base.Init();
        
        // 加载本地引导数据
        LoadLocalGuideData();
        
        LogUtlis.Info("[GuideData] 引导数据管理器初始化完成");
    }

    #endregion

    #region Luban配置加载

    /// <summary>
    /// 从Luban配置表加载引导数据
    /// </summary>
    private List<GuideStepConfig> GetConfigs(int groupId)
    {
        try
        {
            TbGuideConfig table = ConfigManager.Instance.GetVOData<TbGuideConfig>("TbGuideConfig");
            if (table == null || table.DataList == null)
            {
                LogUtlis.Error("[GuideData] TbGuideConfig表为空");
                return null;
            }

            // 筛选该组的所有步骤
            List<GuideStepConfig> steps = new List<GuideStepConfig>();
            foreach (var config in table.DataList)
            {
                if (config.GroupId == groupId)
                {
                    steps.Add(ConvertFromLuban(config));
                }
            }

            if (steps.Count == 0)
            {
                LogUtlis.Warn($"[GuideData] 未找到Luban引导配置: {groupId}");
                return null;
            }

            // 按StepOrder排序
            steps.Sort((a, b) => a.StepOrder.CompareTo(b.StepOrder));
            
            LogUtlis.Info($"[GuideData] 从Luban加载引导配置: {groupId}, 步骤数: {steps.Count}");
            return steps;
        }
        catch (System.Exception e)
        {
            LogUtlis.Error($"[GuideData] 加载Luban配置失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 将Luban配置转换为GuideStepConfig
    /// </summary>
    private GuideStepConfig ConvertFromLuban(GuideConfig guideCfg)
    {
        // 先加载资源配置，以获取GuideType
        GuideResConfig resConfig = null;
        if (guideCfg.ResID > 0)
        {
            resConfig = LoadGuideResConfig(guideCfg.ResID);
        }
        
        // 从资源配置获取GuideType，如果没有资源配置则默认为Click
        int guideType = resConfig != null ? resConfig.GuideType : 1; // 1=Click
        
        GuideStepConfig config = new GuideStepConfig
        {
            // 基础配置
            Id = guideCfg.Id,
            GroupId = guideCfg.GroupId,
            StepOrder = guideCfg.StepOrder,
            ResID = guideCfg.ResID,
            GuideType = (GuideType)guideType,
            TargetPath = guideCfg.TargetPath,
            HighlightType = (HighlightType)guideCfg.HighlightType,
            OffsetX = guideCfg.OffsetX,
            OffsetY = guideCfg.OffsetY,
            ArrowDirection = (ArrowDirection)guideCfg.ArrowDirection,
            AutoNext = guideCfg.AutoNext,
            WaitTime = guideCfg.WaitTime,
            
            // TriggerCondition现在是List<string>
            TriggerCondition = ConvertTriggerCondition(guideCfg.TriggerCondition),
            
            // 根据GuideType推断场景类型
            SceneType = InferSceneType(guideType),
            ForceType = GuideForceType.Strong, // 默认强引导
            
            // 兼容字段
            CustomParam = "",
        };
        
        // 应用资源配置
        if (resConfig != null)
        {
            ApplyResConfig(config, resConfig);
        }
        else if (guideCfg.ResID > 0)
        {
            LogUtlis.Warn($"[GuideData] 未找到资源配置: ResID={guideCfg.ResID}, GuideId={guideCfg.Id}");
        }
        
        return config;
    }

    /// <summary>
    /// 转换触发条件列表为字符串
    /// Luban中的TriggerCondition是List<string>，每个元素是"conditionId:param"格式
    /// 例如: ["1001:5", "1002:100"] 表示条件1001参数5，条件1002参数100
    /// </summary>
    private string ConvertTriggerCondition(System.Collections.Generic.List<string> conditions)
    {
        if (conditions == null || conditions.Count == 0)
            return "";
        
        // 直接用逗号连接，格式: "1001:5,1002:100"
        return string.Join(",", conditions);
    }

    /// <summary>
    /// 根据引导类型推断场景类型
    /// </summary>
    private GuideSceneType InferSceneType(int guideType)
    {
        switch (guideType)
        {
            case 2: // Dialog
                return GuideSceneType.Dialog;
            case 1: // Click
            case 5: // Highlight
                return GuideSceneType.UI;
            default:
                return GuideSceneType.UI;
        }
    }

    /// <summary>
    /// 加载引导资源配置
    /// </summary>
    private cfg.Guide.GuideResConfig LoadGuideResConfig(int resId)
    {
        // 检查缓存
        if (_guideResConfigCache.TryGetValue(resId, out cfg.Guide.GuideResConfig cached))
        {
            return cached;
        }

        try
        {
            // 从Luban配置表加载
            TbGuideResConfig table = ConfigManager.Instance.GetVOData<TbGuideResConfig>("TbGuideResConfig");
            if (table == null)
            {
                LogUtlis.Error("[GuideData] TbGuideResConfig表为空");
                return null;
            }
            
            cfg.Guide.GuideResConfig resConfig = table.GetOrDefault(resId);
            
            if (resConfig != null)
            {
                _guideResConfigCache[resId] = resConfig;
                LogUtlis.Info($"[GuideData] 加载资源配置: ResID={resId}");
                return resConfig;
            }
            else
            {
                LogUtlis.Warn($"[GuideData] 未找到资源配置: ResID={resId}");
                return null;
            }
        }
        catch (System.Exception e)
        {
            LogUtlis.Error($"[GuideData] 加载资源配置失败: ResID={resId}, Error={e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 将资源配置应用到引导步骤配置
    /// </summary>
    private void ApplyResConfig(GuideStepConfig stepConfig, cfg.Guide.GuideResConfig resConfig)
    {
        // 应用表配置字段
        stepConfig.BackgroundImage = resConfig.BackgroundBG;           // 表字段名: BackgroundBG
        stepConfig.CharacterName = resConfig.CharacterName;
        stepConfig.CharacterAvatar = resConfig.CharacterAvatarPath;    // 表字段名: CharacterAvatarPath
        stepConfig.CharacterPos = (CharacterPosition)resConfig.CharacterPosition;
        stepConfig.TipText = resConfig.TipText;
        
        // 箭头图标
        stepConfig.ArrowIcon = resConfig.ArrowIcon;
        
        // 扩展字段表中目前没有的字段，根据需求扩展
        stepConfig.DialogAnimation = "";
        stepConfig.TipTextStyle = "";
        stepConfig.HighlightEffect = "";
        stepConfig.PointerAnimation = "";
        
    }

    #endregion

    #region 本地数据管理

    /// <summary>
    /// 加载本地引导数据
    /// </summary>
    private void LoadLocalGuideData()
    {
        _localGuideData.Clear();
        _guideProgress.Clear();
        
        // 加载已完成的引导
        string data = PlayerPrefs.GetString("GuideCompleted", "");
        if (!string.IsNullOrEmpty(data))
        {
            string[] ids = data.Split(',');
            foreach (string id in ids)
            {
                if (int.TryParse(id, out int groupId))
                {
                    _localGuideData[groupId] = true;
                }
            }
        }
        
        // 加载引导进度（新增）
        string progressData = PlayerPrefs.GetString("GuideProgress", "");
        if (!string.IsNullOrEmpty(progressData))
        {
            string[] progressPairs = progressData.Split(';');
            foreach (string pair in progressPairs)
            {
                string[] kv = pair.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[0], out int groupId) && int.TryParse(kv[1], out int stepIndex))
                {
                    _guideProgress[groupId] = stepIndex;
                }
            }
        }
        
        LogUtlis.Info($"[GuideData] 加载本地引导数据: 已完成{_localGuideData.Count}个, 进度{_guideProgress.Count}个");
    }

    /// <summary>
    /// 保存本地引导数据
    /// </summary>
    private void SaveLocalGuideData()
    {
        // 保存已完成的引导
        List<string> completedIds = new List<string>();
        foreach (var kvp in _localGuideData)
        {
            if (kvp.Value)
            {
                completedIds.Add(kvp.Key.ToString());
            }
        }
        
        string data = string.Join(",", completedIds);
        PlayerPrefs.SetString("GuideCompleted", data);
        
        // 保存引导进度（新增）
        List<string> progressPairs = new List<string>();
        foreach (var kvp in _guideProgress)
        {
            progressPairs.Add($"{kvp.Key}:{kvp.Value}");
        }
        
        string progressData = string.Join(";", progressPairs);
        PlayerPrefs.SetString("GuideProgress", progressData);
        
        PlayerPrefs.Save();
        
        LogUtlis.Info($"[GuideData] 保存本地引导数据: 已完成{completedIds.Count}个, 进度{progressPairs.Count}个");
    }

    /// <summary>
    /// 标记引导完成（本地）
    /// </summary>
    public void MarkGuideCompleted(int groupId)
    {
        _localGuideData[groupId] = true;
        
        // 完成后清除进度记录（新增）
        if (_guideProgress.ContainsKey(groupId))
        {
            _guideProgress.Remove(groupId);
        }
        
        SaveLocalGuideData();
        
        LogUtlis.Info($"[GuideData] 标记引导{groupId}完成（本地）");
    }

    #endregion

    #region 服务器数据管理

    /// <summary>
    /// 接收服务器引导数据
    /// 服务器只推送已完成的引导组列表
    /// </summary>
    /// <param name="serverData">服务器推送的引导数据</param>
    public void ReceiveServerGuideData(Dictionary<int, bool> serverData)
    {
        _serverGuideData.Clear();
        
        if (serverData != null)
        {
            foreach (var kvp in serverData)
            {
                _serverGuideData[kvp.Key] = kvp.Value;
            }
        }
        
        LogUtlis.Info($"[GuideData] 接收服务器引导数据: {_serverGuideData.Count}个已完成的引导组");
    }

    /// <summary>
    /// 向服务器报告引导完成
    /// 只在引导组完成时调用，服务器不关心中间步骤
    /// </summary>
    public void ReportGuideCompleteToServer(int groupId)
    {
        // TODO: 调用网络模块向服务器发送引导完成消息
        // NetworkManager.Instance.SendGuideComplete(groupId);
        
        LogUtlis.Info($"[GuideData] 向服务器报告引导{groupId}完成");
    }

    /// <summary>
    /// 保存引导步骤进度（仅本地）
    /// 服务器不关心步骤级别的进度，只在本地保存用于断点续传
    /// </summary>
    public void SaveGuideProgress(int groupId, int stepIndex)
    {
        _guideProgress[groupId] = stepIndex;
        
        // 保存到本地
        SaveLocalGuideData();
        
        LogUtlis.Info($"[GuideData] 保存引导{groupId}进度: 第{stepIndex}步");
    }

    #endregion

    #region 配置表管理

    /// <summary>
    /// 加载引导配置表
    /// </summary>
    /// <param name="groupId">引导组ID</param>
    /// <returns>引导步骤列表</returns>
    public List<GuideStepConfig> LoadGuideConfig(int groupId)
    {
        // 检查缓存
        if (_guideConfigCache.ContainsKey(groupId))
        {
            LogUtlis.Info($"[GuideData] 从缓存加载引导配置: {groupId}");
            return _guideConfigCache[groupId];
        }

        // 从Luban配置表加载
        List<GuideStepConfig> steps = GetConfigs(groupId);
        if (steps != null && steps.Count > 0)
        {
            _guideConfigCache[groupId] = steps;
            return steps;
        }
        
        // 回退：使用测试数据
        steps = GetTestGuideConfig(groupId);
        
        if (steps != null && steps.Count > 0)
        {
            _guideConfigCache[groupId] = steps;
            LogUtlis.Info($"[GuideData] 加载引导配置: {groupId}, 共{steps.Count}步");
        }
        else
        {
            LogUtlis.Warn($"[GuideData] 未找到引导配置: {groupId}");
        }
        
        return steps;
    }

    /// <summary>
    /// 预加载引导配置（提前缓存）
    /// </summary>
    public void PreloadGuideConfig(int groupId)
    {
        if (!_guideConfigCache.ContainsKey(groupId))
        {
            LoadGuideConfig(groupId);
        }
    }

    /// <summary>
    /// 清除配置缓存
    /// </summary>
    public void ClearConfigCache()
    {
        _guideConfigCache.Clear();
        LogUtlis.Info("[GuideData] 清除引导配置缓存");
    }

    #endregion

    #region 组合数据查询

    /// <summary>
    /// 检查引导是否已完成（综合判断）
    /// 优先服务器数据，其次本地数据
    /// </summary>
    public bool IsGuideCompleted(int groupId)
    {
        // 优先检查服务器数据
        if (_serverGuideData.ContainsKey(groupId))
        {
            return _serverGuideData[groupId];
        }
        
        // 其次检查本地数据
        if (_localGuideData.ContainsKey(groupId))
        {
            return _localGuideData[groupId];
        }
        
        return false;
    }

    /// <summary>
    /// 获取引导进度
    /// </summary>
    public int GetGuideProgress(int groupId)
    {
        if (_guideProgress.ContainsKey(groupId))
        {
            return _guideProgress[groupId];
        }
        
        return 0;
    }

    /// <summary>
    /// 获取所有已完成的引导组ID
    /// </summary>
    public List<int> GetAllCompletedGuideIds()
    {
        HashSet<int> completedIds = new HashSet<int>();
        
        // 合并服务器和本地数据
        foreach (var kvp in _serverGuideData)
        {
            if (kvp.Value)
            {
                completedIds.Add(kvp.Key);
            }
        }
        
        foreach (var kvp in _localGuideData)
        {
            if (kvp.Value)
            {
                completedIds.Add(kvp.Key);
            }
        }
        
        return new List<int>(completedIds);
    }

    /// <summary>
    /// 获取引导数据摘要（用于调试）
    /// </summary>
    public string GetGuideDataSummary()
    {
        return $"服务器数据: {_serverGuideData.Count}个, " +
               $"本地数据: {_localGuideData.Count}个, " +
               $"配置缓存: {_guideConfigCache.Count}个, " +
               $"进度记录: {_guideProgress.Count}个";
    }

    #endregion

    #region 测试数据

    /// <summary>
    /// 获取测试引导配置（临时）
    /// 注意：资源字段现在从GuideResConfig表加载，这里只配置基础逻辑字段
    /// </summary>
    private List<GuideStepConfig> GetTestGuideConfig(int groupId)
    {
        if (groupId == 1001)
        {
            return new List<GuideStepConfig>
            {
                new GuideStepConfig
                {
                    Id = 1,
                    GroupId = 1001,
                    StepOrder = 1,
                    ResID = 10001,  // 对应GuideResConfig表中的对话引导资源
                    GuideType = GuideType.Dialog,
                    SceneType = GuideSceneType.Dialog,
                    ForceType = GuideForceType.Strong,
                    TargetPath = "",
                    HighlightType = HighlightType.None,
                    AutoNext = true,
                    WaitTime = 0
                },
                new GuideStepConfig
                {
                    Id = 2,
                    GroupId = 1001,
                    StepOrder = 2,
                    ResID = 10002,  // 对应GuideResConfig表中的UI点击引导资源
                    GuideType = GuideType.Click,
                    SceneType = GuideSceneType.World,
                    ForceType = GuideForceType.Strong,
                    TargetPath = "MainCity",
                    HighlightType = HighlightType.Circle,
                    ArrowDirection = ArrowDirection.Down,
                    AutoNext = true,
                    WaitTime = 0
                },
                new GuideStepConfig
                {
                    Id = 3,
                    GroupId = 1001,
                    StepOrder = 3,
                    ResID = 10002,
                    GuideType = GuideType.Click,
                    SceneType = GuideSceneType.UI,
                    ForceType = GuideForceType.Weak,
                    TargetPath = "MainWindow/BtnBuild",
                    HighlightType = HighlightType.Rect,
                    ArrowDirection = ArrowDirection.Right,
                    AutoNext = true,
                    WaitTime = 0
                },
                new GuideStepConfig
                {
                    Id = 4,
                    GroupId = 1001,
                    StepOrder = 4,
                    ResID = 10001,
                    GuideType = GuideType.Dialog,
                    SceneType = GuideSceneType.Dialog,
                    ForceType = GuideForceType.Strong,
                    TargetPath = "",
                    HighlightType = HighlightType.None,
                    AutoNext = true,
                    WaitTime = 0
                }
            };
        }
        
        return null;
    }

    #endregion

    #region 清理

    /// <summary>
    /// 清除指定引导的进度
    /// </summary>
    public void ClearGuideProgress(int guideId)
    {
        // 清除本地数据
        if (_localGuideData.ContainsKey(guideId))
        {
            _localGuideData.Remove(guideId);
            SaveLocalGuideData();
        }

        // 清除进度
        if (_guideProgress.ContainsKey(guideId))
        {
            _guideProgress.Remove(guideId);
        }

        // TODO: 通知服务器清除
        LogUtlis.Info($"[GuideData] 清除引导{guideId}的进度");
    }

    /// <summary>
    /// 清除所有引导进度（用于调试）
    /// </summary>
    public void ClearAllGuideProgress()
    {
        _localGuideData.Clear();
        _guideProgress.Clear();
        SaveLocalGuideData();

        // TODO: 通知服务器清除所有
        LogUtlis.Info("[GuideData] 清除所有引导进度");
    }

    /// <summary>
    /// 清空所有数据（销毁时调用）
    /// </summary>
    public void Clear()
    {
        _serverGuideData.Clear();
        _localGuideData.Clear();
        _guideConfigCache.Clear();
        _guideProgress.Clear();
        _guideResConfigCache.Clear();
    }

    #endregion
}
