/// <summary>
/// 条件检测结果
/// </summary>
public class ConditionCheckResult
{
    public bool Success;           // 是否成功
    public string FailMessage;     // 失败提示语
    public int CurrentValue;       // 当前值（用于显示进度）
    public int TargetValue;        // 目标值（用于显示进度）

    public static ConditionCheckResult CreateSuccess(int current = 0, int target = 0)
    {
        return new ConditionCheckResult { Success = true, CurrentValue = current, TargetValue = target };
    }

    public static ConditionCheckResult CreateFail(string message, int current = 0, int target = 0)
    {
        return new ConditionCheckResult { Success = false, FailMessage = message, CurrentValue = current, TargetValue = target };
    }
}

/// <summary>
/// 条件检测器接口
/// 所有具体的条件检测器都需要实现此接口
/// </summary>
public interface IConditionChecker
{
    /// <summary>
    /// 条件类型（用于枚举映射，自定义条件可返回Custom）
    /// </summary>
    ConditionType ConditionType { get; }
    
    /// <summary>
    /// 检查条件是否满足（返回检测结果和失败提示）
    /// </summary>
    /// <param name="lubanConfig">Luban条件配置</param>
    /// <param name="parameters">外部传入的参数（可选，支持多个参数）</param>
    /// <returns>检测结果</returns>
    ConditionCheckResult Check(cfg.Condition.ConditionConfig lubanConfig, params int[] parameters);
}
