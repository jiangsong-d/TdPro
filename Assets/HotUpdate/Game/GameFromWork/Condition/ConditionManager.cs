using System;
using System.Collections.Generic;
using UnityEngine;
using cfg.Condition;

/// <summary>
/// 条件管理器
/// 负责游戏内所有条件的检测判断
/// </summary>
public class ConditionManager : GameSingleton<ConditionManager>
{
    #region 字段

    /// <summary>
    /// Luban条件配置缓存
    /// Key: 条件Key, Value: Luban条件配置
    /// </summary>
    private Dictionary<string, ConditionConfig> _lubanConditionCache = new Dictionary<string, ConditionConfig>();

    /// <summary>
    /// 条件检测器映射（通过脚本名称）
    /// Key: 脚本名称, Value: 检测器实例
    /// </summary>
    private Dictionary<string, IConditionChecker> _checkersByScript = new Dictionary<string, IConditionChecker>();

    #endregion

    #region 初始化

    public override void Init()
    {
        base.Init();
        
        // 注册所有条件检测器
        RegisterCheckers();
        
        LogUtlis.Info("[ConditionManager] 条件系统初始化完成");
    }

    /// <summary>
    /// 注册所有条件检测器（通过反射自动加载）
    /// 遍历配置表，根据ConditionScripts字段自动实例化检测器
    /// </summary>
    private void RegisterCheckers()
    {
        try
        {
            //  加载配置表
            var table = ConfigManager.Instance.GetVOData<cfg.Condition.TbConditionConfig>("TbConditionConfig");
            if (table == null || table.DataList == null)
            {
                LogUtlis.Error("[ConditionManager] TbConditionConfig表为空，无法加载检测器");
                return;
            }

            // 用于去重的集合
            HashSet<string> registeredScripts = new HashSet<string>();

            // 遍历配置表，收集所有脚本名称
            foreach (var config in table.DataList)
            {
                if (string.IsNullOrEmpty(config.ConditionScripts))
                    continue;

                // 去重：同一个脚本只实例化一次
                if (registeredScripts.Contains(config.ConditionScripts))
                    continue;

                // 4. 通过反射创建检测器实例
                IConditionChecker checker = CreateCheckerByReflection(config.ConditionScripts);
                
                if (checker != null)
                {
                    _checkersByScript[config.ConditionScripts] = checker;
                    registeredScripts.Add(config.ConditionScripts);
                    LogUtlis.Info($"[ConditionManager] 自动注册检测器: {config.ConditionScripts}");
                }
            }

            LogUtlis.Info($"[ConditionManager] 通过反射注册了 {_checkersByScript.Count} 个条件检测器");
        }
        catch (System.Exception e)
        {
            LogUtlis.Error($"[ConditionManager] 注册检测器失败: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 通过反射创建检测器实例
    /// </summary>
    /// <param name="scriptName">脚本类名（如：PlayerLevelChecker）</param>
    /// <returns>检测器实例，失败返回null</returns>
    private IConditionChecker CreateCheckerByReflection(string scriptName)
    {
        try
        {
            // 1. 获取类型（在当前程序集中查找）
            System.Type type = System.Type.GetType(scriptName);
            
            // 2. 如果当前程序集找不到，尝试在所有已加载的程序集中查找
            if (type == null)
            {
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(scriptName);
                    if (type != null)
                        break;
                }
            }

            if (type == null)
            {
                LogUtlis.Error($"[ConditionManager] 未找到类型: {scriptName}，请检查类名是否正确");
                return null;
            }

            // 3. 检查类型是否实现了IConditionChecker接口
            if (!typeof(IConditionChecker).IsAssignableFrom(type))
            {
                LogUtlis.Error($"[ConditionManager] 类型 {scriptName} 未实现 IConditionChecker 接口");
                return null;
            }

            // 4. 创建实例
            IConditionChecker checker = System.Activator.CreateInstance(type) as IConditionChecker;
            
            if (checker == null)
            {
                LogUtlis.Error($"[ConditionManager] 创建 {scriptName} 实例失败");
                return null;
            }

            return checker;
        }
        catch (System.Exception e)
        {
            LogUtlis.Error($"[ConditionManager] 反射创建 {scriptName} 失败: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 动态注册自定义条件检测器（供外部扩展使用）
    /// </summary>
    public void RegisterCustomChecker(string scriptName, IConditionChecker checker)
    {
        if (_checkersByScript.ContainsKey(scriptName))
        {
            LogUtlis.Warn($"[ConditionManager] 检测器 {scriptName} 已存在，将被覆盖");
        }
        _checkersByScript[scriptName] = checker;
        LogUtlis.Info($"[ConditionManager] 注册自定义检测器: {scriptName}");
    }
    #endregion

    #region 配置加载

    /// <summary>
    /// 从Luban加载条件配置
    /// </summary>
    private ConditionConfig LoadConditionConfig(string conditionKey)
    {
        // 从缓存获取
        if (_lubanConditionCache.TryGetValue(conditionKey, out var cachedConfig))
        {
            return cachedConfig;
        }

        // 从Luban表加载
        try
        {
            var table = ConfigManager.Instance.GetVOData<cfg.Condition.TbConditionConfig>("TbConditionConfig");
            if (table == null)
            {
                LogUtlis.Error("[ConditionManager] TbConditionConfig表为空");
                return null;
            }

            ConditionConfig config = table.Get(conditionKey);
            if (config == null)
            {
                LogUtlis.Error($"[ConditionManager] 未找到条件配置: {conditionKey}");
                return null;
            }

            // 缓存配置
            _lubanConditionCache[conditionKey] = config;
            return config;
        }
        catch (System.Exception e)
        {
            LogUtlis.Error($"[ConditionManager] 加载条件配置失败: {conditionKey}, 错误: {e.Message}");
            return null;
        }
    }

    #endregion

    #region 条件检测（新接口）

    /// <summary>
    /// 检查条件是否满足（通过Key和参数）
    /// 这是主要的对外接口，供其他系统调用
    /// </summary>
    /// <param name="conditionKey">条件Key（配置表中的Key）</param>
    /// <param name="parameters">参数（可选，支持多个参数）</param>
    /// <returns>检测结果（包含是否成功和失败提示）</returns>
    public ConditionCheckResult CheckCondition(string conditionKey, params int[] parameters)
    {
        // 1. 加载条件配置
        ConditionConfig config = LoadConditionConfig(conditionKey);
        if (config == null)
        {
            return ConditionCheckResult.CreateFail($"条件配置不存在: {conditionKey}");
        }

        // 2. 获取检测器
        if (string.IsNullOrEmpty(config.ConditionScripts))
        {
            return ConditionCheckResult.CreateFail($"条件配置未指定检测脚本: {conditionKey}");
        }

        if (!_checkersByScript.TryGetValue(config.ConditionScripts, out IConditionChecker checker))
        {
            return ConditionCheckResult.CreateFail($"未找到检测器: {config.ConditionScripts}");
        }

        // 3. 执行检测
        ConditionCheckResult result = checker.Check(config, parameters);

        // 4. 日志输出
        if (result.Success)
        {
            LogUtlis.Info($"[ConditionManager] ✓ 条件满足: {conditionKey}, 当前值={result.CurrentValue}, 目标值={result.TargetValue}");
        }
        else
        {
            LogUtlis.Info($"[ConditionManager] ✗ 条件不满足: {conditionKey}, {result.FailMessage}, 当前值={result.CurrentValue}, 目标值={result.TargetValue}");
        }

        return result;
    }

    /// <summary>
    /// 批量检查多个条件（AND逻辑）
    /// </summary>
    public ConditionCheckResult CheckConditions(string[] conditionKeys, int[][] parametersArray = null)
    {
        for (int i = 0; i < conditionKeys.Length; i++)
        {
            int[] params_ = (parametersArray != null && i < parametersArray.Length) ? parametersArray[i] : null;
            ConditionCheckResult result = params_ != null ? CheckCondition(conditionKeys[i], params_) : CheckCondition(conditionKeys[i]);
            
            if (!result.Success)
            {
                return result; // 任意一个失败就返回
            }
        }
        
        return ConditionCheckResult.CreateSuccess();
    }

    /// <summary>
    /// 兼容旧接口：通过conditionId检查
    /// </summary>
    public bool CheckCondition(int conditionId)
    {
        // 将ID转为Key（假设ID就是Key）
        string key = conditionId.ToString();
        ConditionCheckResult result = CheckCondition(key);
        return result.Success;
    }

    #endregion

    #region 销毁

    public override void OnDestroy()
    {
        base.OnDestroy();
        _lubanConditionCache.Clear();
        _checkersByScript.Clear();
    }

    #endregion
}
