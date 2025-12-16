using UnityEngine;
using cfg.Condition;

/// <summary>
/// 条件检测器基类
/// 新增条件时继承此类，只需实现具体的检测逻辑
/// </summary>
public abstract class BaseConditionChecker : IConditionChecker
{
    /// <summary>
    /// 条件类型（用于枚举映射）
    /// 如果是自定义条件，可以返回 ConditionType.Custom
    /// </summary>
    public virtual ConditionType ConditionType => ConditionType.Custom;
    
    /// <summary>
    /// 检查条件是否满足（核心方法，子类必须实现）
    /// </summary>
    /// <param name="lubanConfig">Luban条件配置</param>
    /// <param name="parameters">外部传入的参数（支持多个参数）</param>
    /// <returns>检测结果</returns>
    public abstract ConditionCheckResult Check(cfg.Condition.ConditionConfig lubanConfig, params int[] parameters);
    
    /// <summary>
    /// 创建成功结果
    /// </summary>
    protected ConditionCheckResult Success(int current = 0, int target = 0)
    {
        return ConditionCheckResult.CreateSuccess(current, target);
    }
    
    /// <summary>
    /// 创建失败结果
    /// </summary>
    protected ConditionCheckResult Fail(string message, int current = 0, int target = 0)
    {
        return ConditionCheckResult.CreateFail(message, current, target);
    }
    
    /// <summary>
    /// 创建失败结果
    /// </summary>
    protected ConditionCheckResult FailWithConfig(cfg.Condition.ConditionConfig config, int current = 0, int target = 0)
    {
        return ConditionCheckResult.CreateFail(config.ConditionFailDesc, current, target);
    }
    
    /// <summary>
    /// 比较两个值
    /// </summary>
    protected bool Compare(int current, int target, ConditionCompareType compareType)
    {
        switch (compareType)
        {
            case ConditionCompareType.Equal:
                return current == target;
            case ConditionCompareType.NotEqual:
                return current != target;
            case ConditionCompareType.Greater:
                return current > target;
            case ConditionCompareType.GreaterOrEqual:
                return current >= target;
            case ConditionCompareType.Less:
                return current < target;
            case ConditionCompareType.LessOrEqual:
                return current <= target;
            default:
                return false;
        }
    }
}

