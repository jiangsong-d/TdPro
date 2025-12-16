using cfg.Condition;
/// <summary>
/// VIP等级检测器
/// </summary>
public class PlayerVipLevelChecker : BaseConditionChecker
{
    public override ConditionType ConditionType => ConditionType.PlayerVipLevel;
    
    public override ConditionCheckResult Check(ConditionConfig Config, params int[] parameters)
    {
        int currentLevel = GetCurrentVipLevel();
        int param = parameters != null && parameters.Length > 0 ? parameters[0] : 0;
        int targetLevel = param > 0 ? param : ParseTargetLevel(Config.ActionName);
        
        if (currentLevel >= targetLevel)
        {
            return Success(currentLevel, targetLevel);
        }
        
        return FailWithConfig(Config, currentLevel, targetLevel);
    }
    
    private int GetCurrentVipLevel()
    {
        // TODO: 从PlayerData获取VIP等级
        return 0;
    }
    
    private int ParseTargetLevel(string actionName)
    {
        if (string.IsNullOrEmpty(actionName)) return 0;
        string[] parts = actionName.Split(':');
        if (parts.Length > 1 && int.TryParse(parts[1], out int level))
        {
            return level;
        }
        return 0;
    }
}
